using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace NationalSupportViewer
{
    public static class Program
    {
        private const int CoreCount   = 10;
        private const int ItemPerPage = 10;

        //sido_sgg: 시군구
        //ldongCod: 읍면동
        //zmap_ctgry_code: STEP3

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
            string code;
            do
            {
                Console.Write("법정동코드 입력: ");
                code = Console.ReadLine();

                Console.WriteLine("\n");
            } while (code == null || !int.TryParse(code, out _));

            string zipNo = null;

            for (int i = 0; i < 5; i++)
            {
                zipNo = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtZipNo.do", $"{{\"sido_sgg\":{code[0..5]},\"ldongCod\":{code[5..8]},\"zmap_ctgry_code\":\"00\",\"mcht_nm\":\"\",\"pageNo\":1,\"pageSet\":10}}");

                if (zipNo != null) break;
            }

            if (zipNo == null) return;

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

            var countData = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtCnt.do", $"{{\"zip_no\":\"{rslt}\",\"zmap_ctgry_code\":\"00\",\"mcht_nm\":\"\"}}");
            var count     = int.Parse(countData);

            //LoadSingle(count, rslt);
            LoadMulti(count, rslt);
        }

        private static void LoadMulti(int count, StringBuilder rslt)
        {
            var thr       = new Thread[CoreCount];
            var pageCount = count / ItemPerPage;

            var ttemp = (double)pageCount / CoreCount;

            for (var i = 0; i < thr.Length; i++)
            {
                var i1 = i;
                thr[i] = new Thread(() =>
                {
                    for (int j = (int)(ttemp * i1) + 1; j < (int)(ttemp * (i1 + 1)) + 1; j++)
                    {
                        var tmp = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtInfo.do", $"{{\"zip_no\":\"{rslt}\",\"zmap_ctgry_code\":\"00\",\"mcht_nm\":\"\",\"pageNo\":\"{j}\",\"pageSet\":\"10\"}}");

                        for (int k = 0; k < ItemPerPage; k++)
                        {
                            Console.WriteLine($"이름    : {tmp.Split("mcht_nm\":\"")[k     + 1].Split('"')[0]}\n" +
                                              $"카테고리: {tmp.Split("zmap_ctgry_nm\":\"")[k + 1].Split('"')[0]}\n" +
                                              $"주소    : {tmp.Split("mcht_addr\":\"")[k   + 1].Split('"')[0]}\n");
                        }
                    }
                });
            }

            foreach (var thread in thr)
            {
                thread.Start();
            }

            foreach (var thread in thr)
            {
                thread.Join();
            }
        }

        private static void LoadSingle(int count, StringBuilder rslt)
        {
            var pageCount = count / ItemPerPage;

            for (int j = 1; j < pageCount; j++)
            {
                var tmp = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtInfo.do", $"{{\"zip_no\":\"{rslt}\",\"zmap_ctgry_code\":\"00\",\"mcht_nm\":\"\",\"pageNo\":\"{j}\",\"pageSet\":\"10\"}}");

                for (int k = 0; k < ItemPerPage; k++)
                {
                    Console.WriteLine($"이름    : {tmp.Split("mcht_nm\":\"")[k     + 1].Split('"')[0]}\n" +
                                      $"카테고리: {tmp.Split("zmap_ctgry_nm\":\"")[k + 1].Split('"')[0]}\n" +
                                      $"주소    : {tmp.Split("mcht_addr\":\"")[k   + 1].Split('"')[0]}\n");
                }
            }
        }
    }
}
