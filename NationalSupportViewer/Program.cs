using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NationalSupportViewer
{
    public static class Program
    {
        private static string XmlHttpRequest(string url, string content)
        {
            HttpWebRequest rq   = null;
            WebResponse    resp = null;
            try
            {
                string returnVal;
                var    param = Encoding.UTF8.GetBytes(content);

                rq               = WebRequest.CreateHttp(url);
                rq.UserAgent     = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";
                rq.Method        = "POST";
                rq.ContentLength = param.Length;
                rq.ContentType   = "application/json";

                using (var s = rq.GetRequestStream())
                {
                    s.Write(param, 0, param.Length);
                }

                resp = rq.GetResponse();
                using (var s = resp.GetResponseStream())
                {
                    if (s == null) return null;

                    int count;
                    var buffer = new byte[1024];
                    var bytes  = new List<byte>();
                    do
                    {
                        count = s.Read(buffer, 0, buffer.Length);
                        bytes.AddRange(buffer[..count]);
                    } while (count > 0);

                    var result = bytes.ToArray();
                    returnVal = Encoding.UTF8.GetString(result, 0, result.Length);
                }

                return returnVal;
            }
            catch
            {
                return null;
            }
            finally
            {
                rq?.Abort();
                resp?.Close();
            }
        }

        private static void Main()
        {
            var zipNo = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtZipNo.do", "{\"sido_sgg\":44133,\"ldongCod\":104,\"zmap_ctgry_code\":1,\"mcht_nm\":\"\",\"pageNo\":1,\"pageSet\":10}");

            var temp = zipNo.Split(":\"");
            var zipn = new string[temp.Length];
            for (int i = 1; i < zipn.Length; i++)
            {
                zipn[i] = temp[i].Split('"')[0];
            }

            var rslt = new StringBuilder();

            rslt.Append('[');

            for (var i = 1; i < zipn.Length; i++)
            {
                rslt.Append("\\\"").Append(zipn[i]).Append("\\\"").Append(',');
            }

            rslt.Remove(rslt.Length - 1, 1);
            rslt.Append(']');

            var count = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtCnt.do", $"{{\"zip_no\":\"{rslt}\",\"zmap_ctgry_code\":\"00\",\"mcht_nm\":\"\"}}");

            for (int i = 1; i < (int.Parse(count) / 10) + 1; i++)
            {
                var tmp = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtInfo.do", $"{{\"zip_no\":\"{rslt}\",\"zmap_ctgry_code\":\"00\",\"mcht_nm\":\"\",\"pageNo\":\"{i}\",\"pageSet\":\"10\"}}");

                Console.WriteLine($"이름    : {tmp.Split("mcht_nm\":\"")[1].Split('"')[0]}\n"       +
                                  $"카테고리: {tmp.Split("zmap_ctgry_nm\":\"")[1].Split('"')[0]}\n" +
                                  $"주소    : {tmp.Split("mcht_addr\":\"")[1].Split('"')[0]}\n");
            }
        }
    }
}
