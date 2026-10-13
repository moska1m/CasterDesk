namespace CronoLive;
// Stable grid order from the user's Siege menu references (30 September 2026).
internal static class OperatorOrder
{
    internal static readonly string[] Attack=["striker","sledge","thatcher","ash","thermite","twitch","montagne","glaz","fuze","blitz","iq","buck","blackbeard","capitao","hibana","jackal","ying","zofia","dokkaebi","lion","finka","maverick","nomad","gridlock","nokk","amaru","kali","iana","ace","zero","flores","osa","sens","grim","brava","ram","deimos","rauora","solid-snake"];
    internal static readonly string[] Defense=["sentry","smoke","mute","castle","pulse","doc","rook","kapkan","tachanka","jager","bandit","frost","valkyrie","caveira","echo","mira","lesion","ela","vigil","alibi","maestro","clash","kaid","mozzie","warden","goyo","wamai","oryx","melusi","aruni","thunderbird","thorn","azami","solis","fenrir","tubarao","skopos","denari","noor"];
    internal static IEnumerable<SiegeOperator> Sort(IEnumerable<SiegeOperator> operators,string role)
    {
        var order=role=="ATQ"?Attack:Defense;
        return operators.Where(o=>o.Role==role).OrderBy(o=>{int n=Array.IndexOf(order,o.Id);return n<0?int.MaxValue:n;});
    }
}
