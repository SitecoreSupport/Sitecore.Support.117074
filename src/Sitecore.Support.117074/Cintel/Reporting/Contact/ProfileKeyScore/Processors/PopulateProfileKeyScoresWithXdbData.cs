namespace Sitecore.Support.Cintel.Reporting.Contact.ProfileKeyScore.Processors
{
  using System.Data;

  [UsedImplicitly]
  public class PopulateProfileKeyScoresWithXdbData : Sitecore.Cintel.Reporting.Contact.ProfileKeyScore.Processors.PopulateProfileKeyScoresWithXdbData
  {
    public override void Process(Sitecore.Cintel.Reporting.ReportProcessorArgs reportArguments)
    {                                              
      base.Process(reportArguments);

      var resultTableForView = reportArguments.ResultSet.Data.Dataset[reportArguments.ReportParameters.ViewName];
      ReplaceNaN(resultTableForView);
      reportArguments.ResultSet.Data.Dataset[reportArguments.ReportParameters.ViewName] = resultTableForView;
    }

    private void ReplaceNaN(DataTable dataTable)
    {
      if (dataTable == null)
      {
        return;
      }

      foreach (DataRow row in dataTable.Rows)
      {
        var value = row[Sitecore.Cintel.Reporting.Contact.ProfileKeyScore.Schema.ProfileKeyValue.Name];
        if (value != null && value.ToString() == "NaN")
        {
          row[Sitecore.Cintel.Reporting.Contact.ProfileKeyScore.Schema.ProfileKeyValue.Name] = 0.0;
        }
      }
    }
  }
}
