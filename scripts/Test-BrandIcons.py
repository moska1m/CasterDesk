"""Read-only PE resource verification; never executes the EXEs."""
import struct
import sys
from pathlib import Path

ico = Path(sys.argv[1]).read_bytes()
count = struct.unpack_from('<H', ico, 4)[0]
expected = []
for i in range(count):
    entry = struct.unpack_from('<BBBBHHII', ico, 6 + 16 * i)
    expected.append((entry[:6], ico[entry[7]:entry[7] + entry[6]]))

for filename in sys.argv[2:]:
    data = Path(filename).read_bytes()
    pe = struct.unpack_from('<I', data, 60)[0]
    sections, opt_size = struct.unpack_from('<H', data, pe + 6)[0], struct.unpack_from('<H', data, pe + 20)[0]
    opt = pe + 24
    directory = opt + (112 if struct.unpack_from('<H', data, opt)[0] == 0x20b else 96)
    resource = struct.unpack_from('<I', data, directory + 16)[0]
    def offset(rva):
        for s in range(sections):
            header = opt + opt_size + s * 40
            virtual_size, address, raw_size, raw = struct.unpack_from('<IIII', data, header + 8)
            if address <= rva < address + max(virtual_size, raw_size):
                return raw + rva - address
        raise ValueError('RVA outside sections')
    base = offset(resource)
    resources = {}
    def walk(relative, names=()):
        at = base + relative
        named, ids = struct.unpack_from('<HH', data, at + 12)
        for i in range(named + ids):
            key, value = struct.unpack_from('<II', data, at + 16 + i * 8)
            path = names + (key,)
            if value & 0x80000000:
                walk(value & 0x7fffffff, path)
            else:
                rva, size = struct.unpack_from('<II', data, base + value)
                resources[path] = data[offset(rva):offset(rva) + size]
    walk(0)
    matched = False
    for key, group in resources.items():
        if key[0] != 14 or struct.unpack_from('<H', group, 4)[0] != count:
            continue
        actual = []
        for i in range(count):
            entry = struct.unpack_from('<BBBBHHIH', group, 6 + i * 14)
            payload = next(v for k, v in resources.items() if k[0] == 3 and k[1] == entry[7] and k[2] == key[2])
            actual.append((entry[:6], payload))
        if actual == expected:
            matched = True
            break
    if not matched:
        raise ValueError('Approved icon not found: ' + filename)
    print(f'PASS: all {count} approved ICO resolutions embedded byte-for-byte in {Path(filename).name}')
