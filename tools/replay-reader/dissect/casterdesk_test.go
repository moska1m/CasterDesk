package dissect

import (
	"encoding/binary"
	"testing"
)

func TestCasterDeskCountersPreserveSnapshotAndUnknown(t *testing.T) {
	entity:=[]byte{1,2,3,4}
	field:=func(marker []byte,value uint32) []byte { b:=append([]byte{},marker...);b=append(b,4);b=binary.LittleEndian.AppendUint32(b,value);return append(b,0x22) }
	b:=append(append([]byte{},entity...),0,0,0,0)
	b=append(b,field(scoreboardKillsField,5)...)
	b=append(b,field(scoreboardDeathsField,3)...)
	b=append(b,field([]byte{0x4D,0x73,0x7F,0x9E},2)...)
	r:=&Reader{b:b,scoreboardEntities:map[string]*scoreboardEntity{string(entity):{username:"Player"}}}
	r.EnableCasterDeskCounters()
	for i:=0;i<3;i++ {r.offset=12+10*i;if err:=r.listeners[i][0](r);err!=nil{t.Fatal(err)}}
	c:=r.CasterDeskCounters["Player"]
	if c==nil||c.Kills==nil||*c.Kills!=5||c.InitialKills==nil||*c.InitialKills!=5||c.Deaths==nil||*c.Deaths!=3||c.InitialAssists==nil||*c.InitialAssists!=2 {t.Fatalf("snapshot not preserved: %+v",c)}
	// A later kills-only update must not erase the baseline or invent other fields.
	r.b=append(append(append([]byte{},entity...),0,0,0,0),field(scoreboardKillsField,6)...)
	r.offset=12;_ = r.listeners[0][0](r)
	if *c.Kills!=6||*c.InitialKills!=5||*c.Assists!=2{t.Fatal("update overwrote baseline")}
	r.scoreboardEntities[string(entity)].username="NewPlayer"
	_ = r.listeners[0][0](r)
	unknown:=r.CasterDeskCounters["NewPlayer"]
	if unknown.InitialKills!=nil||unknown.Assists!=nil||unknown.Deaths!=nil{t.Fatal("missing counter became zero")}
}
