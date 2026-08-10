// FIXTURE ONLY - not real application code.
// Exists so the validator has real files and real line counts to check
// citations against. Line numbers here are referenced by the fixture docs.
using System;

namespace Fixture.Billing
{
    public partial class OrderApproval : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!User.IsInRole("Manager"))
            {
                Response.Redirect("~/AccessDenied.aspx");
            }
            if (!IsPostBack)
            {
                BindOrder();
            }
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            if (Order.Total > 10000 && !Order.HasSecondApproval)
            {
                lblError.Text = "Orders over $10,000 require a second approval.";
                return;
            }

            if (!CheckStock(Order.Id))
            {
                lblError.Text = "Stock unavailable.";
                return;
            }

            var threshold = ConfigurationManager.AppSettings["Approval." + Order.Region];
            OrderService.Approve(Order.Id, threshold);
            Response.Redirect("~/Billing/OrderList.aspx");
        }
    }
}
