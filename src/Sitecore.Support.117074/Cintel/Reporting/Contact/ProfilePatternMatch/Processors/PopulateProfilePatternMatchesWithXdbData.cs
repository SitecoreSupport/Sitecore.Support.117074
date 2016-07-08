namespace Sitecore.Support.Cintel.Reporting.Contact.ProfilePatternMatch.Processors
{
  using Sitecore.Analytics;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Analytics.Patterns;
  using Sitecore.Cintel.Reporting.Processors;
  using Sitecore.Data;
  using Sitecore.Diagnostics;
  using System;
  using System.Collections.Generic;
  using System.Data;
  using System.Linq;
  using Sitecore.Cintel.Reporting;
  using Sitecore.Cintel.Reporting.Contact.ProfilePatternMatch;

  [UsedImplicitly]
  public class PopulateProfilePatternMatchesWithXdbData : ReportProcessorBase
  {
    public static System.Collections.Generic.Dictionary<PatternCardItem, double> GetPatternsWithGravityShare(ProfileItem profileContainingPatterns, Pattern referencePattern)
    {
      Dictionary<PatternCardItem, double> patternsWithGravityShare;
      var totalGravity = CalculateGravityForAllPatterns(profileContainingPatterns, referencePattern, out patternsWithGravityShare);
      patternsWithGravityShare = GetPatternsWithGravityShare(patternsWithGravityShare, totalGravity);

      return patternsWithGravityShare;
    }


    public override void Process(Sitecore.Cintel.Reporting.ReportProcessorArgs args)
    {
      var viewEntityId = Guid.Parse(args.ReportParameters.ViewEntityId);
      Assert.IsFalse(viewEntityId == Guid.Empty, "ViewEntityId must be a valid GUID");

      var profileItem = Tracker.DefinitionItems.Profiles[viewEntityId];
      Assert.IsNotNull(profileItem, "Could not find profile with id [{0}]", viewEntityId);

      var queryResult = args.QueryResult;
      var patternsWithGravityShare = GetPatternsWithGravityShare(profileItem, GetPatternForInteraction(profileItem, queryResult));
      var resultTableForView = args.ResultTableForView;

      ProjectPatternsIntoResultTable(args, patternsWithGravityShare, resultTableForView, viewEntityId);
    }

    private static double CalculateGravityForAllPatterns(ProfileItem profileContainingPatterns, Pattern referencePattern, out System.Collections.Generic.Dictionary<PatternCardItem, double> patternsWithCalculatedGravityShare)
    {
      var num = 0.0;
      patternsWithCalculatedGravityShare = new System.Collections.Generic.Dictionary<PatternCardItem, double>();
      foreach (PatternCardItem current in profileContainingPatterns.Patterns)
      {
        var referencePattern2 = current.CreatePattern(profileContainingPatterns.PatternSpace);
        var num2 = CalculateGravityForPattern(referencePattern2, referencePattern);
        num += num2;
        patternsWithCalculatedGravityShare.Add(current, num2);
      }

      return num;
    }

    private static double CalculateGravityForPattern(Pattern referencePattern, Pattern interactionPattern)
    {
      var num = new SquaredEuclidianDistance().GetDistance(interactionPattern, referencePattern);
      if (num.CompareTo(0.0) == 0)
      {
        num = 1E-27;
      }

      return 1.0 / num;
    }

    public class SquaredEuclidianDistance : IPatternDistance
    {
      public double GetDistance(Pattern a, Pattern b)
      {
        Assert.IsTrue(a.Space == b.Space, "Patterns are from different spaces");

        var distance = 0.0;
        for (int i = 0; i < a.Space.Dimensions; i++)
        {
          var num3 = a[i] - b[i];
          distance = distance + num3 * num3;
        }

        return distance;
      }
    }

    private static Pattern GetPatternForInteraction(ProfileItem profile, DataTable interactionValues)
    {
      var array = new double[profile.PatternSpace.Dimensions];
      var num = 1;

      foreach (var current in interactionValues.AsEnumerable())
      {
        num = current.Field<int>("Count");
        var patternKeyAndValueFromProfileValue = GetPatternKeyAndValueFromProfileValue(current);

        array[profile.PatternSpace.GetKeyIndex(patternKeyAndValueFromProfileValue.Key)] = patternKeyAndValueFromProfileValue.Value;
      }

      if (profile.Type.ToLowerInvariant() == "percentage")
      {
        var num2 = array.Sum(d => d);
        for (var i = 0; i < array.Length; i++)
        {
          array[i] /= num2;
        }
      }
      else if (profile.Type.ToLowerInvariant() == "average")
      {
        for (var j = 0; j < array.Length; j++)
        {
          array[j] /= num;
        }
      }

      return new Pattern(profile.PatternSpace, array);
    }

    private static KeyValuePair<string, double> GetPatternKeyAndValueFromProfileValue(DataRow profileValue)
    {
      var profileKeyId = profileValue.Field<Guid>("ProfileKeyId");
      var itemId = new ID(profileKeyId);
      var key = Context.Database.GetItem(itemId).Key;
      var value = profileValue.Field<double>("Score");

      return new KeyValuePair<string, double>(key, value);
    }

    private static Dictionary<PatternCardItem, double> GetPatternsWithGravityShare(Dictionary<PatternCardItem, double> patternsWithCalculatedGravity, double totalGravity)
    {
      var dictionary = new Dictionary<PatternCardItem, double>(patternsWithCalculatedGravity.Count);
      foreach (PatternCardItem current in patternsWithCalculatedGravity.Keys)
      {
        var dictionaryCurrent = patternsWithCalculatedGravity[current] / totalGravity;
        if (double.IsNaN(dictionaryCurrent))
        {
          dictionaryCurrent = 0.0;
        }

        dictionary[current] = dictionaryCurrent;
      }

      return dictionary;
    }

    private static void ProjectPatternsIntoResultTable(ReportProcessorArgs args, Dictionary<PatternCardItem, double> patternsWithGravity, DataTable resultTable, System.Guid profileId)
    {
      foreach (var current in patternsWithGravity)
      {
        var dataRow = resultTable.NewRow();

        dataRow[Schema.ContactId.Name] = args.ReportParameters.ContactId;
        dataRow[Schema.LatestVisitId.Name] = args.ReportParameters.AdditionalParameters["VisitId"];
        dataRow[Schema.ProfileId.Name] = profileId;
        dataRow[Schema.PatternId.Name] = current.Key.ID.Guid;
        dataRow[Schema.PatternDisplayName.Name] = current.Key.DisplayName;
        dataRow[Schema.PatternGravityShare.Name] = current.Value;

        resultTable.Rows.Add(dataRow);
      }
    }


  }
}
