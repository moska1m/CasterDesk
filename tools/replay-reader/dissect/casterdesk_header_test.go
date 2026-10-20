package dissect

import (
	"bytes"
	"io"
	"strings"
	"testing"

	"github.com/klauspost/compress/zstd"
)

func TestCasterDeskHeaderWithoutRoundBody(t *testing.T) {
	var fixture bytes.Buffer
	fixture.WriteString("dissect")
	fixture.Write(make([]byte, 7))
	fixture.WriteByte(1)
	fixture.Write(make([]byte, 7))
	fields := [][2]string{
		{"version", "Y11S3"}, {"code", "99999999"}, {"datetime", "2026-10-17-13-13-03"},
		{"matchtype", "1"}, {"worldid", "126631055"}, {"recordingplayerid", "1"},
		{"gamemodeid", "1"}, {"roundspermatch", "12"}, {"roundspermatchovertime", "3"},
		{"roundnumber", "0"}, {"overtimeroundnumber", "0"}, {"teamname0", "NA ORG"},
		{"teamname1", "FaZe"}, {"id", "e0d4e818-1234-4567-8901-123456789012"},
		{"startingteamscore0", "0"}, {"startingteamscore1", "0"}, {"teamscore0", "0"}, {"teamscore1", "0"},
	}
	for _, pair := range fields {
		for _, text := range pair {
			fixture.WriteByte(byte(len(text)))
			fixture.Write(make([]byte, 7))
			fixture.WriteString(text)
		}
	}
	fixture.WriteByte(0) // Header terminator; no round event stream follows.
	for _, compressed := range []bool{false, true} {
		data := fixture.Bytes()
		if compressed {
			encoder, err := zstd.NewWriter(nil)
			if err != nil {
				t.Fatal(err)
			}
			data = encoder.EncodeAll(data, nil)
			encoder.Close()
		}
		header, err := ReadCasterDeskHeader(bytes.NewReader(data))
		if err != nil {
			t.Fatalf("compressed=%v: %v", compressed, err)
		}
		if header.Teams[0].Name != "NA ORG" || header.Teams[1].Name != "FaZe" || header.Timestamp.Year() != 2026 {
			t.Fatalf("incorrect metadata: %+v", header)
		}
	}
	// Reading the label must not consume or decompress the full replay body.
	input := &countedHeaderReader{Reader: io.MultiReader(bytes.NewReader(fixture.Bytes()), strings.NewReader(strings.Repeat("x", 2<<20)))}
	if _, err := ReadCasterDeskHeader(input); err != nil {
		t.Fatal(err)
	}
	if input.bytes > 1<<20 {
		t.Fatalf("read %d bytes beyond header limit", input.bytes)
	}
	if _, err := ReadCasterDeskHeader(strings.NewReader("broken replay")); err == nil {
		t.Fatal("invalid replay accepted")
	}
}

type countedHeaderReader struct {
	io.Reader
	bytes int
}

func (r *countedHeaderReader) Read(b []byte) (int, error) {
	n, err := r.Reader.Read(b)
	r.bytes += n
	return n, err
}
