Imports Util40

Public Class DBFunction

   Public Shared Function GetTxnInfo(SOId As String, conn As ConnectionStringSettings) As DataTable
      Dim SelectSQL As String = ""

      SelectSQL &= "select a.LocId as MerchantId, a.CurId, ISNULL(b.value,'') as ZIPChargeId "
      SelectSQL &= "from tblSON a (nolock) left join tblPaymentInfo b (Nolock) on a.SOId = b.SOId and b.type = 'ChargeId' and b.SOCreditCardType = 'ZIP' "
      SelectSQL &= "where a.soid = ? "

      Dim paramList As New List(Of SqlLibParameter)()
      paramList.Add(New SqlLibParameter("@SOId", DbType.String, SOId))

      Return SqlLib.ExecuteDataTable(CommandType.Text, SelectSQL, paramList, conn)
   End Function


   Public Shared Function UpdateRefundHKBTxn(QSICode As String, DRTxnId As String, BatchNo As String, Remark As String, RefundTxnId As String, conn As ConnectionStringSettings) As Boolean
      Dim UpdateSQL As String = ""

      If (Remark.Length > 50) Then
         Remark = Left(Remark, 50)
      End If

      Dim paramList As New List(Of SqlLibParameter)()

      If (QSICode = "5") Then
         UpdateSQL &= "Update tblRefundHKBTxn Set QSICode = ?, pgDRTxnId = null, pgBatchNo= null, remark = ? where refundTxnId = ? "
         paramList.Add(New SqlLibParameter("@QSICode", DbType.Int16, QSICode))
         paramList.Add(New SqlLibParameter("@remark", DbType.String, Remark))
         paramList.Add(New SqlLibParameter("@refundTxnId", DbType.Int32, RefundTxnId))
      Else
         UpdateSQL &= "Update tblRefundHKBTxn Set QSICode = ?, pgDRTxnId = ?, pgBatchNo= ?, remark = ? where refundTxnId = ? "
         paramList.Add(New SqlLibParameter("@QSICode", DbType.Int16, QSICode))
         paramList.Add(New SqlLibParameter("@pgDRTxnId", DbType.Int32, DRTxnId))
         paramList.Add(New SqlLibParameter("@pgBatchNo", DbType.Int32, BatchNo))
         paramList.Add(New SqlLibParameter("@remark", DbType.String, Remark))
         paramList.Add(New SqlLibParameter("@refundTxnId", DbType.Int32, RefundTxnId))
      End If


      Try
         SqlLib.ExecuteNonQuery(CommandType.Text, UpdateSQL, paramList, conn)
      Catch ex As Exception
         Return False
      End Try

      Return True
   End Function

End Class
