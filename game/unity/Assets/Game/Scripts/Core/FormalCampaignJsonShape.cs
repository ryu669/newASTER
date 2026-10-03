using System;
using System.Globalization;
using System.Text;
namespace NewAster.Core
{
    // Presence only: payload validation remains the serializer's responsibility.
    public static class FormalCampaignJsonShape
    {
        public static bool HasRootMember(string json,string member)
            =>FindRootMemberValue(json,member)>=0;
        public static bool RootMemberIsNull(string json,string member)
        {
            int index=FindRootMemberValue(json,member);
            if(index<0 || index+4>json.Length || json.Substring(index,4)!="null")return false;
            return index+4==json.Length || char.IsWhiteSpace(json[index+4]) || json[index+4]==',' || json[index+4]=='}';
        }
        private static int FindRootMemberValue(string json,string member)
        {
            if(json==null)throw new ArgumentNullException(nameof(json));int depth=0;
            for(int i=0;i<json.Length;i++) {
                char c=json[i];
                if(c=='{' || c=='['){depth++;continue;}
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
