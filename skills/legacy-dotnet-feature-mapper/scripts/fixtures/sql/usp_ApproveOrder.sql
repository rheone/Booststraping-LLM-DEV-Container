-- FIXTURE ONLY - not real application SQL.
CREATE PROCEDURE dbo.usp_ApproveOrder
    @OrderId INT,
    @ApprovedBy NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Orders
       SET Status      = 'Approved',
           ApprovedBy  = @ApprovedBy,
           ApprovedUtc = SYSUTCDATETIME()
     WHERE OrderId = @OrderId
       AND Status = 'Pending';

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('Order not in Pending state.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.OrderHistory (OrderId, Action, ActionUtc)
    VALUES (@OrderId, 'Approved', SYSUTCDATETIME());
END
