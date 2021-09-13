using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace NationalSupportViewer
{
    public static class Program
    {
        private static readonly int CoreCount = Math.Max(1, Environment.ProcessorCount);
        private const int ItemPerPage = 10;

        //sido_sgg: 시군구
        //ldongCod: 읍면동
        //zmap_ctgry_code: STEP3

        private static string XmlHttpRequest(string url, string content)
        {
            HttpWebRequest req  = null;
            WebResponse    resp = null;

            try
            {
                string retVal;

                var param = Encoding.UTF8.GetBytes(content);

                req               = WebRequest.CreateHttp(url);
                req.UserAgent     = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";
                req.Method        = "POST";
                req.ContentLength = param.Length;
                req.ContentType   = "application/json";

                using (var reqStream = req.GetRequestStream())
                {
                    reqStream.Write(param, 0, param.Length);
                }

                resp = req.GetResponse();
                using (var respStream = resp.GetResponseStream())
                {
                    if (respStream == null) return null;

                    int count;
                    var buffer = new byte[1024];
                    var bytes  = new List<byte>();
                    do
                    {
                        count = respStream.Read(buffer, 0, buffer.Length);
                        bytes.AddRange(buffer[..count]);
                    } while (count > 0);

                    var result = bytes.ToArray();
                    retVal = Encoding.UTF8.GetString(result, 0, result.Length);
                }

                return retVal;
            }
            catch
            {
                return null;
            }
            finally
            {
                req?.Abort();
                resp?.Close();
            }
        }

        private static void Main()
        {
            string legalLocalCode;
            do
            {
                Console.Write("(도로명 기준)법정동코드 입력: ");
                legalLocalCode = Console.ReadLine();

                Console.WriteLine("\n");
            } while (legalLocalCode == null || !double.TryParse(legalLocalCode, out _));

            string zipCode = null;

            for (int i = 0; i < 5; i++)
            {
                zipCode = GetZipData(legalLocalCode);
                if (zipCode != null) break;
            }

            if (zipCode == null)
            {
                Console.WriteLine("해당 법정동코드의 지역 번호를 가져올 수 없습니다.");
                return;
            }

            var countData = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtCnt.do",
                                           $"{{\"zip_no\":\"{zipCode}\","
                                           + "\"zmap_ctgry_code\":\"00\","
                                           + "\"mcht_nm\":\"\"}");

            //LoadSingle(count, rslt);
            LoadMulti(int.Parse(countData), zipCode);
        }

        private static string GetZipData(string legalLocalCode)
        {
            string zipData = null;

            for (int i = 0; i < 5; i++)
            {
                zipData = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtZipNo.do",
                                         $"{{\"sido_sgg\":{legalLocalCode[..5]},"
                                         + $"\"ldongCod\":{legalLocalCode[5..8]},"
                                         + "\"zmap_ctgry_code\":\"00\","
                                         + "\"mcht_nm\":\"\","
                                         + "\"pageNo\":1,"
                                         + "\"pageSet\":10}");

                if (zipData != null) break;
            }

            if (zipData == null) return null;

            var temp    = zipData.Split(":\"");
            var zipCode = new string[temp.Length];
            for (int i = 1; i < zipCode.Length; i++)
            {
                zipCode[i] = temp[i].Split('"')[0];
            }

            var zipToStr = new StringBuilder();

            zipToStr.Append('[');

            for (var i = 1; i < zipCode.Length; i++)
            {
                zipToStr.Append("\\\"").Append(zipCode[i]).Append("\\\"").Append(',');
            }

            zipToStr.Remove(zipToStr.Length - 1, 1);
            zipToStr.Append(']');

            return zipToStr.ToString();
        }

        private static void LoadMulti(int count, string zipCode)
        {
            var threads = new Thread[CoreCount];

            var pageCount   = count / ItemPerPage;
            var pagePerCore = (double)pageCount / CoreCount;

            var result = new StringBuilder();

            for (var i = 0; i < threads.Length; i++)
            {
                var i1 = i;
                threads[i] = new Thread(() =>
                {
                    for (int j = (int)(pagePerCore * i1) + 1; j < (int)(pagePerCore * (i1 + 1)) + 1; j++)
                    {
                        var data = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtInfo.do",
                                                  $"{{\"zip_no\":\"{zipCode}\","
                                                  + "\"zmap_ctgry_code\":\"00\","
                                                  + "\"mcht_nm\":\"\","
                                                  + $"\"pageNo\":\"{j}\","
                                                  + "\"pageSet\":\"10\"}");

                        for (int k = 1; k < ItemPerPage + 1; k++)
                        {
                            var text = $"이름    : {data.Split("mcht_nm\":\"")[k].Split('"')[0]}\n"
                                     + $"카테고리: {data.Split("zmap_ctgry_nm\":\"")[k].Split('"')[0]}\n"
                                     + $"주소    : {data.Split("mcht_addr\":\"")[k].Split('"')[0]}\n\n";

                            Console.Write(text);
                            result.Append(text);
                        }
                    }
                });
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            File.WriteAllText("result.txt", result.ToString());
        }

        //private static void LoadSingle(int count, StringBuilder rslt)
        //{
        //    var pageCount = count / ItemPerPage;

        //    for (int j = 1; j < pageCount; j++)
        //    {
        //        var tmp = XmlHttpRequest("https://xn--3e0bnl907agre90ivg11qswg.kr/whereToUse/getMchtInfo.do", $"{{\"zip_no\":\"{rslt}\",\"zmap_ctgry_code\":\"00\",\"mcht_nm\":\"\",\"pageNo\":\"{j}\",\"pageSet\":\"10\"}}");

        //        for (int k = 0; k < ItemPerPage; k++)
        //        {
        //            Console.WriteLine($"이름    : {tmp.Split("mcht_nm\":\"")[k     + 1].Split('"')[0]}\n" +
        //                              $"카테고리: {tmp.Split("zmap_ctgry_nm\":\"")[k + 1].Split('"')[0]}\n" +
        //                              $"주소    : {tmp.Split("mcht_addr\":\"")[k   + 1].Split('"')[0]}\n");
        //        }
        //    }
        //}
    }
}
