Public Class CreateRefundRequest
   Public charge_id As String
   Public reason As String
   Public amount As Decimal

   Public Sub New(_charge_id As String, _reason As String, _amount As Decimal)
      charge_id = _charge_id
      reason = _reason
      amount = _amount
   End Sub

End Class
