using System;
using System.Globalization;
using System.Text;
namespace NewAster.Core
{
    // Presence only: payload validation remains the serializer's responsibility.
    public static class FormalCampaignJsonShape
    {
        public sealed class RootMembers
        {
            private readonly string json;
            private readonly System.Collections.Generic.Dictionary<string,(int start,int end)> values=new System.Collections.Generic.Dictionary<string,(int,int)>(StringComparer.Ordinal);
            internal RootMembers(string json)
            {
                this.json=json??throw new ArgumentNullException(nameof(json));int i=0;
                while(i<json.Length && char.IsWhiteSpace(json[i]))i++;
                if(i>=json.Length || json[i++]!='{')return;
                while(i<json.Length){
                    while(i<json.Length && (char.IsWhiteSpace(json[i]) || json[i]==','))i++;
                    if(i>=json.Length || json[i++]!='"')return;
                    var key=new StringBuilder();bool closed=false;
                    while(i<json.Length){char c=json[i++];if(c=='"'){closed=true;break;}
                        if(c=='\\'){
                            if(i>=json.Length)return;c=json[i++];
                            if(c=='u'){if(i+4>json.Length || !int.TryParse(json.Substring(i,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out int code))return;key.Append((char)code);i+=4;continue;}
                            c=c=='n'?'\n':c=='r'?'\r':c=='t'?'\t':c=='b'?'\b':c=='f'?'\f':c;
                        }key.Append(c);
                    }
                    if(!closed)return;while(i<json.Length && char.IsWhiteSpace(json[i]))i++;
                    if(i>=json.Length || json[i++]!=':')return;while(i<json.Length && char.IsWhiteSpace(json[i]))i++;
                    int end=EndOfValue(json,i);string name=key.ToString();if(!values.ContainsKey(name))values.Add(name,(i,end));
                    if(end<=i)return;i=end;
                }
            }
            public bool Contains(string member)=>values.ContainsKey(member);
            public bool IsNull(string member)=>values.TryGetValue(member,out var span) && span.end-span.start==4 && string.CompareOrdinal(json,span.start,"null",0,4)==0;
            public string Json(string member)=>values.TryGetValue(member,out var span)?json.Substring(span.start,span.end-span.start):null;
            internal string Nulls(string[] members)
            {
                var spans=new System.Collections.Generic.List<(int start,int end)>();
                foreach(string member in members)if(values.TryGetValue(member,out var span) && !spans.Contains(span))spans.Add(span);
                if(spans.Count==0)return json;spans.Sort((a,b)=>a.start.CompareTo(b.start));
                var result=new StringBuilder(json.Length);int from=0;
                foreach(var span in spans){result.Append(json,from,span.start-from);result.Append("null");from=span.end;}
                result.Append(json,from,json.Length-from);return result.ToString();
            }
        }
        public static RootMembers Inspect(string json)=>new RootMembers(json);
        public static string WithNullRootMembers(string json,params string[] members)=>Inspect(json).Nulls(members);
        public static bool HasRootMember(string json,string member)
            =>FindRootMemberValue(json,member)>=0;
        public static int? RootInt32Member(string json,string member)
        {
            int start=FindRootMemberValue(json,member);if(start<0)return null;int end=start;
            if(end<json.Length && json[end]=='-')end++;while(end<json.Length && json[end]>='0' && json[end]<='9')end++;
            if(end==start || end<json.Length && !char.IsWhiteSpace(json[end]) && json[end]!=',' && json[end]!='}')return null;
            return int.TryParse(json.Substring(start,end-start),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int value)?value:(int?)null;
        }
        public static bool RootMemberIsNull(string json,string member)
        {
            int index=FindRootMemberValue(json,member);
            if(index<0 || index+4>json.Length || json.Substring(index,4)!="null")return false;
            return index+4==json.Length || char.IsWhiteSpace(json[index+4]) || json[index+4]==',' || json[index+4]=='}';
        }
        // Used only on serializer-produced JSON: Unity's inline serialization can
        // turn a null serializable object into a default object. Preserve true absence.
        public static string WithNullRootMember(string json,string member)=>WithRootMemberJson(json,member,"null");
        public static string RootMemberJson(string json,string member)
        {
            int start=FindRootMemberValue(json,member);return start<0?null:json.Substring(start,EndOfValue(json,start)-start);
        }
        // Replacement is a fragment produced by the serializer, never arbitrary user text.
        public static string WithRootMemberJson(string json,string member,string replacement)
        {
            int start=FindRootMemberValue(json,member);if(start<0)return json;
            return json.Substring(0,start)+replacement+json.Substring(EndOfValue(json,start));
        }
        private static int EndOfValue(string json,int start)
        {
            int depth=0;bool quoted=false,escaped=false;int end=start;
            for(;end<json.Length;end++){
                char c=json[end];
                if(quoted){if(escaped){escaped=false;continue;}if(c=='\\'){escaped=true;continue;}if(c=='"')quoted=false;continue;}
                if(c=='"'){quoted=true;continue;}
                if(c=='{' || c=='['){depth++;continue;}
                if(c=='}' || c==']'){if(depth==0)break;depth--;if(depth==0){end++;break;}continue;}
                if(depth==0 && (c==',' || char.IsWhiteSpace(c)))break;
            }
            return end;
        }
        private static int FindRootMemberValue(string json,string member)
        {
            if(json==null)throw new ArgumentNullException(nameof(json));int depth=0;
            for(int i=0;i<json.Length;i++) {
                char c=json[i];
                // Nested histories cannot contain root keys; skip strings without allocating them.
                if(c=='{' || c=='['){if(depth==1){i=EndOfValue(json,i)-1;continue;}depth++;continue;}
                if(c=='}' || c==']'){depth--;continue;}
                if(c!='"')continue;
                var token=new StringBuilder();bool closed=false;
                while(++i<json.Length) {
                    c=json[i];if(c=='"'){closed=true;break;}
                    if(c=='\\') {
                        if(++i>=json.Length)break;c=json[i];
                        if(c=='u') {
                            if(i+4>=json.Length || !int.TryParse(json.Substring(i+1,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out int code))break;
                            token.Append((char)code);i+=4;continue;
                        }
                        c=c=='n'?'\n':c=='r'?'\r':c=='t'?'\t':c=='b'?'\b':c=='f'?'\f':c;
                    }
                    token.Append(c);
                }
                if(!closed)return -1;
                int next=i+1;while(next<json.Length && char.IsWhiteSpace(json[next]))next++;
                if(depth==1 && next<json.Length && json[next]==':' && token.ToString()==member) {
                    next++;while(next<json.Length && char.IsWhiteSpace(json[next]))next++;return next;
                }
            }
            return -1;
        }
    }
}
