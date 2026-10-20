// Local-only CasterDesk adapter. No HTTP, authentication or upload code.
package main

import (
	"encoding/json"
	"fmt"
	"os"
	"runtime/debug"
	"github.com/redraskal/r6-dissect/dissect"
	"github.com/rs/zerolog"
)

func main() {
	defer func(){ if err:=recover(); err!=nil { fmt.Fprintln(os.Stderr,"Replay incompatible:",err); os.Exit(2) } }()
	debug.SetMemoryLimit(768<<20)
	debug.SetGCPercent(50)
	zerolog.SetGlobalLevel(zerolog.Disabled)
	if len(os.Args)!=2 { fmt.Fprintln(os.Stderr,"Expected one .rec file"); os.Exit(1) }
	f,err:=os.Open(os.Args[1]); if err!=nil { fail(err) }; defer f.Close()
	r,err:=dissect.NewReader(f); if err!=nil { fail(err) }
	r.EnableCasterDeskCounters()
	if err=r.Read(); !dissect.Ok(err) { fail(err) }
	output:=struct {
		Schema int `json:"schema"`
		Reader string `json:"reader"`
		dissect.Header
		Events []dissect.MatchUpdate `json:"events"`
		Stats []dissect.PlayerRoundStats `json:"stats"`
		Counters map[string]*dissect.CasterDeskCounter `json:"counters"`
	}{1,"julio208920/r6-dissect@6602cf2e + CasterDesk counters",r.Header,r.MatchFeedback,r.PlayerStats(),r.CasterDeskCounters}
	if err=json.NewEncoder(os.Stdout).Encode(output);err!=nil { fail(err) }
}
func fail(err error){ fmt.Fprintln(os.Stderr,err);os.Exit(1) }
