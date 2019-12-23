Imports System.IO
Imports System.Net
Imports log4net
Imports Newtonsoft.Json

Public Class RefundTxn
   Inherits System.Web.UI.Page

   Protected logger As ILog

   Private SOId As String = ""
   Private CurId As String = ""
   Private RefundAmt As String = ""
   Private RefundTxnId As String = ""

   Private conn As ConnectionStringSettings

   Private AUDCurId As String = "AUD"


   Protected Sub InitLogger()

      log4net.GlobalContext.Properties("LogName") = "RefundTxn"
      log4net.GlobalContext.Properties("ApplicationName") = "RefundTxn"

      Dim log4netPath = Server.MapPath("~/log4net.config")
      log4net.Config.XmlConfigurator.ConfigureAndWatch(New System.IO.FileInfo(log4netPath))

      logger = LogManager.GetLogger("RefundTxn")

   End Sub



   Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

      conn = ConfigurationManager.ConnectionStrings("NewMainDb")
      InitLogger()



      SOId = Trim(Request.QueryString("soid") & " ")
      CurId = Trim(Request.QueryString("curId") & " ")
      RefundAmt = Request.QueryString("refundamt")
      RefundTxnId = Request.QueryString("refundtxnid")
      logger.Debug("--------------------------------------------")
      logger.Debug("SOID=" & SOId)
      logger.Debug("CurId=" & CurId)
      logger.Debug("RefundAmt=" & RefundAmt)
      logger.Debug("RefundTxnId=" & RefundTxnId)



      If (SOId = "") Then
         Response.Write("Failed Invalid SOId <br/>")

         logger.Error("Invalid SOId")
         Return
      End If

      If (CurId <> AUDCurId) Then
         Response.Write("Failed Invalid CurId <br/>")
         logger.Error("Invalid CurId")
         Return
      End If

      Dim dt As DataTable = DBFunction.GetTxnInfo(SOId, conn)

      If (dt.Rows.Count > 0) Then
         Dim ZipChargeId As String = dt.Rows(0).Item("ZIPChargeId")


         ' Testing Order
         'If SOId = "1970866586" AndAlso ZipChargeId = "" Then
         '   ZipChargeId = "ch_jjGuzmQ9tYDI1SJzCxjBT4"
         'End If
         ' Testing Order


         If (ZipChargeId = "") Then
            Response.Write("Failed")
            Response.Write("ZIP Charge Id not found!<br/>")
            logger.Error("ZIP Charge Id not found!")
            Return
         End If

         CreateRefund(ZipChargeId)

      Else
         Response.Write("Failed")
         Response.Write("Order not found <br/>")
         logger.Error("Order not found!")
      End If
   End Sub


   Public Function CreateRefund(chargeId As String) As Boolean
      Dim APIRefundURL As String = ConfigurationManager.AppSettings("APIRefundURL")
      Dim APIAuthorization As String = ConfigurationManager.AppSettings("APIAuthorization")
      Dim APIVersion As String = ConfigurationManager.AppSettings("APIVersion")

      Dim RefundRequest As CreateRefundRequest = New CreateRefundRequest(chargeId, "Refund", RefundAmt)



      Dim json As String = JsonConvert.SerializeObject(RefundRequest)

      ' Dim json As String = String.Format("\{""charge_id"":""{0}"",""reason"":""Refund"",""amount"":{1}\}", chargeId, RefundAmt)

      Dim encoding As System.Text.Encoding = System.Text.Encoding.UTF8



      Dim data As Byte() = encoding.UTF8.GetBytes(json)

      Dim WebReq = DirectCast(WebRequest.Create(APIRefundURL), HttpWebRequest)

      WebReq.ContentType = "application/json; charset=utf-8"
      WebReq.Headers.Add("Authorization", APIAuthorization)
      WebReq.Headers.Add("Zip-Version", APIVersion)


      WebReq.ContentLength = data.Length
      WebReq.Method = "POST"
      WebReq.KeepAlive = True
      WebReq.Proxy.Credentials = CredentialCache.DefaultCredentials


      Dim stream As Stream = WebReq.GetRequestStream()
      If data.Length > 0 Then
         stream.Write(data, 0, data.Length)
         stream.Close()
      End If

      Dim errorMsg As String = ""
      Dim Success As Boolean = False

      Dim WebRes As HttpWebResponse

      Try
         WebRes = DirectCast(WebReq.GetResponse(), HttpWebResponse)
      Catch we As WebException
         Success = False
         logger.Error(we.Message.ToString())
         WebRes = we.Response
      End Try



      If (Not WebRes Is Nothing) Then

         Dim responseString As String = ""
         Using streamReader = New StreamReader(WebRes.GetResponseStream(), encoding)
            responseString = streamReader.ReadToEnd()
         End Using

         logger.Debug("responseString=" & responseString)

         If (WebRes.StatusCode = HttpStatusCode.OK Or WebRes.StatusCode = HttpStatusCode.Created) Then
            logger.Info("response.statusCode=" & WebRes.StatusCode)
            logger.Info("response.StatusDescription=" & WebRes.StatusDescription)
            Success = True
            Dim jsonResponse = JsonConvert.DeserializeObject(responseString)
            logger.Info("API Return Success!")
            errorMsg = ""
         Else
            logger.Error("response.statusCode=" & WebRes.StatusCode)
            logger.Error("response.StatusDescription=" & WebRes.StatusDescription)
            Success = False
            logger.Error("API Return Failed!")

            Try
               If (responseString.StartsWith("{""error"":") AndAlso responseString.EndsWith("}")) Then

                  responseString = responseString.Substring("{""error"":".Length)
                  responseString = responseString.Substring(0, responseString.Length - 1)

                  Dim errorElement = JsonConvert.DeserializeObject(responseString)

                  logger.Error("error.Code=" & errorElement("code"))
                  logger.Error("error.message=" & errorElement("message"))

                  errorMsg = errorElement("message")
               End If

            Catch ex As Exception
               errorMsg = ex.Message.ToString()

            End Try

         End If


      Else
         Success = False

         logger.Error("API Return Nothing!")
         errorMsg = "API Return Nothing!"
      End If



      Dim QSICode As String = ""
      Dim Remark As String = ""
      Dim BatchNo As String = ""
      Dim DRTxnId As String = ""

      If (Success) Then
         QSICode = "0"
         remark = ""
         BatchNo = DateTime.Now.ToString("yyyymmdd")
         DRTxnId = SOId


         If (DBFunction.UpdateRefundHKBTxn(QSICode, DRTxnId, BatchNo, Remark, RefundTxnId, conn)) Then
            Response.Write("Done")
         Else
            Response.Write("Failed API return success However, update Refund HKB Txn table error!")
         End If


      Else
         QSICode = "5"
         Remark = errorMsg
         BatchNo = ""
         DRTxnId = ""

         DBFunction.UpdateRefundHKBTxn(QSICode, DRTxnId, BatchNo, Remark, RefundTxnId, conn)

         Response.Write("Failed " & errorMsg)
      End If


      Return Success
   End Function





End Class