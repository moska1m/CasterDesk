package dissect

import (
	"bufio"
	"io"

	"github.com/klauspost/compress/zstd"
)

// ReadCasterDeskHeader reads a bounded prefix, without decoding round events.
// Modern replays store this header before the compressed chunks. Older replays
// need only the first decompressed bytes, never the complete round.
func ReadCasterDeskHeader(in io.Reader) (Header, error) {
	br := bufio.NewReader(in)
	chunked, err := testFileCompression(br)
	if err != nil {
		return Header{}, err
	}
	var source io.Reader = br
	if !chunked {
		decoder, err := zstd.NewReader(br, zstd.WithDecoderConcurrency(1), zstd.WithDecoderMaxMemory(64<<20))
		if err != nil {
			return Header{}, err
		}
		defer decoder.Close()
		source = decoder
	}
	r := &Reader{}
	r.b, err = io.ReadAll(io.LimitReader(source, 1<<20))
	if err != nil && len(r.b) == 0 {
		return Header{}, err
	}
	if err := r.readHeaderMagic(); err != nil {
		return Header{}, err
	}
	return r.readHeader()
}
