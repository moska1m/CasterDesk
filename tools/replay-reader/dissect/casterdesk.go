package dissect

// CasterDesk keeps the cumulative scoreboard separately from event-derived stats.
// Missing counters remain nil: missing data is never treated as a zero.
type CasterDeskCounter struct {
	Kills *uint32 `json:"kills"`
	Deaths *uint32 `json:"deaths"`
	Assists *uint32 `json:"assists"`
	InitialKills *uint32 `json:"initialKills"`
	InitialDeaths *uint32 `json:"initialDeaths"`
	InitialAssists *uint32 `json:"initialAssists"`
}

func (r *Reader) EnableCasterDeskCounters() {
	r.CasterDeskCounters = make(map[string]*CasterDeskCounter)
	for _, field := range []struct { marker []byte; kind int }{
		{scoreboardKillsField, 0}, {scoreboardDeathsField, 1}, {[]byte{0x4D,0x73,0x7F,0x9E},2},
	} {
		kind := field.kind
		r.Listen(field.marker, func(r *Reader) error {
			e, snapshot := r.scoreboardEntityAt(r.offset-4)
			if e == nil { return nil }
			value, ok := r.scoreboardValue(); if !ok { return nil }
			c := r.CasterDeskCounters[e.username]
			if c == nil { c = &CasterDeskCounter{}; r.CasterDeskCounters[e.username]=c }
			v := value
			switch kind {
			case 0: c.Kills=&v; if snapshot && c.InitialKills==nil { first:=v; c.InitialKills=&first }
			case 1: c.Deaths=&v; if snapshot && c.InitialDeaths==nil { first:=v; c.InitialDeaths=&first }
			case 2: c.Assists=&v; if snapshot && c.InitialAssists==nil { first:=v; c.InitialAssists=&first }
			}
			return nil
		})
	}
}
