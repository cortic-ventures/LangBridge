using LangBridge.ContextualBridging;
using LangBridge.Extensions;
using LangBridge.Tests.Integration.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using LangBridge.Internal.Infrastructure.Processing;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace LangBridge.Tests.Integration.TextContextualBridge;

/// <summary>
/// Complex extraction showcase tests demonstrating advanced inference capabilities.
/// These tests serve as both capability demonstrations and usage examples.
/// </summary>
public class ComplexExtractionShowcaseTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITextContextualBridge _bridge;
    private readonly TestConfiguration _testConfiguration;

    public ComplexExtractionShowcaseTests(ITestOutputHelper output)
    {
        TestConfigurationHelper.SkipIfAiModelsNotConfigured("ComplexShowcase");
        _output = output;
        _testConfiguration = new TestConfiguration();
        
        var services = new ServiceCollection();
        
        // Use the configuration from TestConfiguration
        services.AddSingleton<IConfiguration>(_testConfiguration.Configuration);
        services.AddLangBridge(_testConfiguration.Configuration);
        
        _serviceProvider = services.BuildServiceProvider();
        _bridge = _serviceProvider.GetRequiredService<ITextContextualBridge>();
    }

    #region 1. Financial Analysis Extraction

    [Fact, Trait("Category", "ComplexShowcase")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_FinancialAnalysis_FromNarrativeText()
    {
        // Arrange - Complex narrative with indirect financial indicators
        var narrative = @"
            TechCorp's CEO mentioned during the all-hands that 'despite the stormy weather 
            in the tech sector, we're weathering it better than most.' She highlighted that 
            while competitors have reduced headcount by 20-30%, TechCorp only implemented a 
            hiring freeze. 'Our runway has actually extended compared to Q2,' she noted, 
            'thanks to the new enterprise deals.' The CFO's smile when discussing the 
            'significantly improved unit economics' suggested stronger margins. Several 
            employees noticed the expansion of the sales team despite the freeze, and the 
            unusual mid-year bonus payout has everyone speculating about a potential acquisition 
            or IPO preparation. The fact that the company moved from monthly to quarterly 
            all-hands meetings was seen as a sign of stability, not crisis management.";
        
        // Act
        var result = await _bridge.ExtractAsync<FinancialAnalysis>(
            narrative, 
            "Extract financial analysis including business trajectory, competitive position, growth indicators, and potential corporate actions");
        
        // Assert
        Assert.True(result.IsSuccess, $"Extraction failed: {(result.IsFailure? result.Error: "N/A")}");
        Assert.NotNull(result.Value);
        
        _output.WriteLine($"Overall Trajectory: {result.Value.OverallTrajectory}");
        _output.WriteLine($"Competitive Position: {result.Value.CompetitivePosition:P}");
        _output.WriteLine($"Growth Stage: {result.Value.GrowthStage}");
        
        // Verify key inferences
        Assert.Equal("Positive", result.Value.OverallTrajectory);
        Assert.True(result.Value.CompetitivePosition >= 0.7, "Should infer strong competitive position");
        var corporateActions = string.Join(", ", result.Value.PotentialCorporateActions ?? new List<string>());
        Assert.True(corporateActions.Contains("IPO", StringComparison.OrdinalIgnoreCase) || 
                   corporateActions.Contains("acquisition", StringComparison.OrdinalIgnoreCase), 
                   "Should identify either IPO or acquisition as potential corporate action");
        
        // Log detailed results
        _output.WriteLine("\nInferred Metrics:");
        foreach (var metric in result.Value.InferredMetrics)
        {
            _output.WriteLine($"  {metric.Key}: {metric.Value}");
        }
    }

    #endregion

    #region 2. Event Timeline Reconstruction

    [Fact, Trait("Category", "ComplexShowcase")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_EventTimeline_FromOutOfOrderNarrative()
    {
        // Arrange - Story with events mentioned out of chronological order
        var narrative = @"
            Sarah mentioned she finally submitted the patent application yesterday, which was 
            exactly six months after the breakthrough discovery. That discovery happened two 
            weeks after she joined the research team. The team had been working on the problem 
            for three months before Sarah arrived. The beta test results came in last Tuesday, 
            showing promising outcomes from the prototype we built in September. Oh, and the 
            prototype was based on Sarah's initial design, which she sketched out during her 
            first week. The funding approval came through in early August, which allowed us 
            to hire Sarah in the first place. Next month's conference presentation will mark 
            exactly one year since the project kickoff. The patent lawyer said the review 
            process typically takes 3-4 months, so we're hoping for approval by spring.";
        
        // Act
        var result = await _bridge.ExtractAsync<EventTimeline>(
            narrative,
            "Reconstruct the chronological timeline of events, inferring dates and sequence order from relative time references");
        
        // Assert
        Assert.True(result.IsSuccess, $"Extraction failed: {(result.IsFailure? result.Error: "N/A")}");
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Events);
        
        _output.WriteLine($"\nReconstructed Timeline ({result.Value.Events.Count} events):");
        foreach (var evt in result.Value.Events.OrderBy(e => e.SequenceOrder))
        {
            _output.WriteLine($"{evt.SequenceOrder}. {evt.Description}");
            if (evt.InferredDate.HasValue)
                _output.WriteLine($"   Date: {evt.InferredDate:yyyy-MM-dd} (Confidence: {evt.DateConfidence})");
        }
        
        // Verify chronological ordering and key events
        var orderedEvents = result.Value.Events.OrderBy(e => e.SequenceOrder).ToList();
        Assert.Contains(orderedEvents, e => e.Description.Contains("project kickoff", StringComparison.OrdinalIgnoreCase) || 
                                           e.Description.Contains("kickoff", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(orderedEvents, e => e.Description.Contains("funding", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(orderedEvents, e => e.Description.Contains("conference", StringComparison.OrdinalIgnoreCase));
        
        // Verify funding comes before Sarah joining (logical ordering)
        var fundingEvent = orderedEvents.FirstOrDefault(e => e.Description.Contains("funding", StringComparison.OrdinalIgnoreCase));
        var sarahEvent = orderedEvents.FirstOrDefault(e => e.Description.Contains("sarah", StringComparison.OrdinalIgnoreCase) && 
                                                           (e.Description.Contains("join", StringComparison.OrdinalIgnoreCase) || 
                                                            e.Description.Contains("hire", StringComparison.OrdinalIgnoreCase)));
        if (fundingEvent != null && sarahEvent != null)
        {
            Assert.True(fundingEvent.SequenceOrder < sarahEvent.SequenceOrder, "Funding should come before Sarah joining");
        }
    }

    #endregion

    #region 3. Relationship Network Extraction

    [Fact, Trait("Category", "ComplexShowcase")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_RelationshipNetwork_FromSocialNarrative()
    {
        // Arrange - Complex social dynamics narrative
        var narrative = @"
            At the quarterly planning meeting, everyone waited for Marcus to speak first, 
            though technically Jennifer runs the engineering department. Tom and Sarah 
            exchanged knowing glances when the budget was mentioned - they'd clearly 
            discussed this before. Marcus deferred to Chen on all technical matters, which 
            surprised the new hires who didn't know Chen used to be CTO before voluntarily 
            stepping back to a principal engineer role for family reasons. 
            
            Jennifer seemed frustrated when Marcus cut her off mid-presentation, but she 
            maintained her composure. Later, I overheard Tom telling Sarah that Jennifer 
            had been promised Marcus's role but the board brought him in from outside instead. 
            
            The dynamics shifted noticeably when Lisa from product joined - both Marcus and 
            Jennifer straightened up and became more formal. Even Chen, usually relaxed, 
            chose his words carefully around her. Sarah whispered to Tom that Lisa has the 
            CEO's ear and once got a VP fired for missing quarterly targets.
            
            During the break, the junior engineers clustered around Chen, peppering him with 
            questions, while the senior staff gave Marcus a wide berth. Jennifer spent the 
            break on her phone, probably talking to her mentor Rachel, the former CTO who 
            now works at a competitor.";
        
        
        
        // Act
        var result = await _bridge.ExtractAsync<RelationshipNetwork>(
            narrative,
            "Extract the social network including people, their roles, relationships, power dynamics, and influence levels");
        // Assert
        Assert.True(result.IsSuccess, $"Extraction failed: {(result.IsFailure ? result.Error : "N/A")}");
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.People);
        Assert.NotEmpty(result.Value.Relationships);
        
        _output.WriteLine($"\nExtracted Network: {result.Value.People.Count} people, {result.Value.Relationships.Count} relationships");
        
        // Verify key power dynamics
        var lisa = result.Value.People.FirstOrDefault(p => p.Name.Contains("Lisa", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(lisa);
        
        var lisaInfluence = result.Value.InfluenceScores.GetValueOrDefault(lisa.Name, 0);
        Assert.True(lisaInfluence > 0.8, "Lisa should have high influence score");
        
        // Log influence hierarchy
        _output.WriteLine("\nInfluence Hierarchy:");
        foreach (var (person, score) in result.Value.InfluenceScores.OrderByDescending(kv => kv.Value))
        {
            _output.WriteLine($"  {person}: {score:F2}");
        }
        
        // Verify relationship types - check for any conflict/tension/hierarchical relationships
        /*var conflictRelationships = result.Value.Relationships.Where(r => 
            r.Type.Contains("Tension", StringComparison.OrdinalIgnoreCase) ||
            r.Type.Contains("Conflict", StringComparison.OrdinalIgnoreCase) ||
            r.Type.Contains("Hierarchical", StringComparison.OrdinalIgnoreCase) ||
            r.Type.Contains("Power", StringComparison.OrdinalIgnoreCase)); 
            commented this because we need to provide the accepted values in the description so that the llm knows the boundaries for the values
            */
    }

    #endregion

    #region 4. Product Comparison Analysis

    [Fact, Trait("Category", "ComplexShowcase")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_ProductComparison_FromReviewNarrative()
    {
        // Arrange - Unstructured product review with implicit comparisons
        var narrative = @"
            I've been using DataFlow Pro for six months after switching from AnalyticsMaster. 
            While AnalyticsMaster was definitely faster at basic queries - I'm talking 2-3x 
            faster on simple aggregations - DataFlow Pro absolutely shines when you need 
            complex transformations. My team adapted to DataFlow's interface within a week, 
            whereas AnalyticsMaster took nearly a month for everyone to feel comfortable.
            
            Cost-wise, DataFlow seems expensive at $299/month until you factor in AnalyticsMaster's 
            hidden fees. We were paying $199/month base, but add $50 for advanced exports, 
            $30 for API access, and another $40 for priority support, and you're actually 
            paying more. Plus, DataFlow includes all those features standard.
            
            The real game-changer is DataFlow's Python integration. AnalyticsMaster claims 
            to support Python, but it's just a basic wrapper. DataFlow lets you write actual 
            pandas code that executes natively. This alone saved us 15-20 hours per month.
            
            That said, if you're just doing basic reporting, AnalyticsMaster is probably 
            fine. Their template library is more extensive, and the learning curve is gentler. 
            DataFlow assumes you know what you're doing. Also, AnalyticsMaster's customer 
            support is friendlier, even if DataFlow's is more technically competent.
            
            For enterprise features, DataFlow wins hands down. Real-time collaboration, 
            git-style version control, and proper staging environments. AnalyticsMaster's 
            'enterprise' plan is basically just their regular plan with SSO tacked on.";
        
        // Act
        var result = await _bridge.ExtractAsync<ProductComparison>(
            narrative,
            "Extract detailed product comparison including features, pricing, strengths, weaknesses, and recommendations");
        
        // Assert
        Assert.True(result.IsSuccess, $"Extraction failed: {(result.IsFailure? result.Error:"N/A")}");
        Assert.NotNull(result.Value);
        
        _output.WriteLine($"Extracted {result.Value.Products?.Count ?? 0} products");
        _output.WriteLine($"Extracted {result.Value.Dimensions?.Count ?? 0} dimensions");
        
        if (result.Value.Products?.Any() == true)
        {
            foreach (var product in result.Value.Products)
            {
                _output.WriteLine($"Product: {product.Name} (Score: {product.OverallScore})");
            }
        }
        
        if (result.Value.Dimensions?.Any() == true)
        {
            foreach (var dimension in result.Value.Dimensions)
            {
                _output.WriteLine($"Dimension: {dimension.Key}");
            }
        }
        
        Assert.NotEmpty(result.Value.Products ?? new List<ProductComparison.Product>());
        Assert.NotEmpty(result.Value.Dimensions ?? new Dictionary<string, ProductComparison.ComparisonDimension>());
        
        _output.WriteLine($"\nExtracted {result.Value.Products?.Count ?? 0} products with {result.Value.Dimensions?.Count ?? 0} comparison dimensions");
        
        // Verify product identification
        Assert.Equal(2, result.Value.Products?.Count ?? 0);
        Assert.Contains(result.Value.Products ?? new List<ProductComparison.Product>(), p => p.Name.Contains("DataFlow", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Value.Products ?? new List<ProductComparison.Product>(), p => p.Name.Contains("AnalyticsMaster", StringComparison.OrdinalIgnoreCase));
        
        // Log comparison matrix
        _output.WriteLine("\nComparison Matrix:");
        foreach (var dimension in result.Value.Dimensions ?? new Dictionary<string, ProductComparison.ComparisonDimension>())
        {
            _output.WriteLine($"\n{dimension.Key} (Weight: {dimension.Value.ImportanceWeight:F2}):");
            foreach (var score in dimension.Value.ProductScores)
            {
                _output.WriteLine($"  {score.Key}: {score.Value:F2}");
            }
            _output.WriteLine($"  Winner: {dimension.Value.Winner}");
        }
        
        _output.WriteLine($"\nRecommendation: {result.Value.RecommendedProduct}");
        _output.WriteLine($"Rationale: {result.Value.RecommendationRationale}");
    }

    #endregion

    #region 5. Cognitive Process Analysis

    [Fact, Trait("Category", "ComplexShowcase")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_CognitiveProfile_FromProblemSolvingNarrative()
    {
        // Arrange - Detailed observation of problem-solving behavior
        var narrative = @"
            When presented with the system architecture problem, Alex immediately started 
            sketching boxes and arrows on the whiteboard. 'Let's map out what we know,' 
            he said, but then paused mid-drawing. 'Actually, wait. I'm jumping ahead. 
            What are our actual constraints here?'
            
            He spent the next ten minutes asking clarifying questions, occasionally 
            muttering 'but what if...' under his breath. I noticed he kept returning to 
            the performance requirements, circling that section of the document three times.
            
            'OK, so we have three possible approaches,' Alex began, then stopped himself 
            again. 'No, actually four. Maybe five if we consider...' He drew five columns 
            on the whiteboard and started listing pros and cons for each. Halfway through 
            the third approach, he erased everything and started over with a different 
            organization scheme.
            
            'I might be overthinking this,' he said, but continued adding more detail. 
            When asked about his preference, he said 'I'm leaning 70% toward option 2, 
            but I'd really like to prototype options 2 and 3 before committing. Can we 
            get the team's input first? Especially Karen - she's dealt with similar 
            scaling issues before.'
            
            Throughout the session, Alex referenced two previous projects, drew analogies 
            to biological systems twice, and kept a running list of 'assumptions to validate' 
            in the corner of the board. When pressured for a quick decision, he said 
            'I can give you a direction now, but I'd be much more confident with 2 days 
            to research and a team review. The cost of being wrong here is pretty high.'
            
            Interestingly, after the meeting, I saw him at his desk building a quick 
            simulation model, even though he hadn't committed to an approach yet.";
        
        // Act
        var result = await _bridge.ExtractAsync<CognitiveProfile>(
            narrative,
            "Analyze Alex's cognitive patterns, thinking styles, problem-solving approach, and behavioral indicators based on the observed decision-making process");
        
        // Assert
        Assert.True(result.IsSuccess, $"Extraction failed: {(result.IsFailure? result.Error: string.Empty)}");
        
        _output.WriteLine("\nCognitive Profile Analysis:");
        
        // Verify thinking styles
        _output.WriteLine($"\nThinking Styles: {string.Join(", ", result.Value.ThinkingStyles)}");
        Assert.NotEmpty(result.Value.ThinkingStyles);
        Assert.Contains(result.Value.ThinkingStyles, s => s.Contains("Analytical", StringComparison.OrdinalIgnoreCase) || 
                                                          s.Contains("Systematic", StringComparison.OrdinalIgnoreCase));
        
        // Verify observed behaviors
        _output.WriteLine($"\nObserved Behaviors: {string.Join(", ", result.Value.ObservedBehaviors)}");
        Assert.NotEmpty(result.Value.ObservedBehaviors);
        
        // Verify decision-making pattern
        _output.WriteLine($"\nDecision-Making Pattern: {result.Value.PrimaryDecisionMakingPattern}");
        Assert.NotEmpty(result.Value.PrimaryDecisionMakingPattern);
        
        // Log problem-solving strategies
        _output.WriteLine($"\nProblem-Solving Strategies: {string.Join(", ", result.Value.ProblemSolvingStrategies)}");
        Assert.NotEmpty(result.Value.ProblemSolvingStrategies);
        
        // Verify metacognitive indicators
        _output.WriteLine($"\nMetacognitive Indicators: {string.Join(", ", result.Value.MetacognitiveIndicators)}");
        Assert.NotEmpty(result.Value.MetacognitiveIndicators);
        
        // Log approach to uncertainty
        _output.WriteLine($"\nApproach to Uncertainty: {result.Value.ApproachToUncertainty}");
        Assert.NotEmpty(result.Value.ApproachToUncertainty);
        
        // Log communication patterns
        _output.WriteLine($"\nCommunication Patterns: {string.Join(", ", result.Value.CommunicationPatterns)}");
        
        _output.WriteLine($"\nInformation Processing Style: {result.Value.InformationProcessingStyle}");
    }

    #endregion

    public void Dispose()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }
}

#region Complex Type Definitions

public class FinancialAnalysis
{
    [Description("Overall business direction: 'Positive', 'Negative', 'Stable', 'Mixed' based on narrative clues")]
    public string OverallTrajectory { get; set; } = string.Empty;
    
    [Description("Financial metrics that can be inferred like 'HeadcountChange: stable', 'CashRunway: extended', 'Margins: improving'")]
    public Dictionary<string, object> InferredMetrics { get; set; } = new();
    
    [Description("Potential corporate activities mentioned or implied like 'IPO preparation', 'Acquisition target', 'Expansion'")]
    public List<string> PotentialCorporateActions { get; set; } = new();
    
    [Description("Competitive strength from 0.0 to 1.0, where 1.0 is market leader based on comparative statements")]
    public double CompetitivePosition { get; set; }
    
    [Description("Business risks mentioned or implied in the narrative")]
    public List<string> RiskFactors { get; set; } = new();
    
    [Description("Business maturity stage: 'Startup', 'Growth', 'Mature', 'Decline' based on context clues")]
    public string GrowthStage { get; set; } = string.Empty;
    
    [Description("Sentiment of different groups like 'Employees: optimistic', 'Leadership: confident'")]
    public Dictionary<string, string> StakeholderSentiment { get; set; } = new();
}

public class EventTimeline
{
    [Description("List of events extracted from the narrative in chronological order")]
    public List<TimelineEvent> Events { get; set; } = new();
    
    [Description("Optional: Earliest date if any absolute dates can be inferred, otherwise leave null")]
    public DateTime? EarliestInferredDate { get; set; }
    
    [Description("Optional: Latest date if any absolute dates can be inferred, otherwise leave null")]
    public DateTime? LatestInferredDate { get; set; }
    
    [Description("Optional: Duration between events in HH:mm:ss format (e.g., '168:00:00' for 1 week, '720:00:00' for 1 month)")]
    public Dictionary<string, TimeSpan?> InferredDurations { get; set; } = new();
    
    public class TimelineEvent
    {
        [Description("Brief description of what happened")]
        public string Description { get; set; } = string.Empty;
        
        [Description("Optional: Only set if an absolute date can be reasonably inferred with confidence, otherwise leave null")]
        public DateTime? InferredDate { get; set; }
        
        [Description("Confidence level of date inference: 'High', 'Medium', 'Low', or 'Relative only'")]
        public string DateConfidence { get; set; } = string.Empty;
        
        [Description("Optional: Names or descriptions of other events this is related to")]
        public List<string> RelatedEvents { get; set; } = new();
        
        [Description("Chronological order number (1 = earliest, increasing for later events)")]
        public int SequenceOrder { get; set; }
    }
}

public class RelationshipNetwork
{
    [Description("All people mentioned in the narrative")]
    public List<Person> People { get; set; } = new();
    
    [Description("Relationships between people that can be inferred from the narrative")]
    public List<Relationship> Relationships { get; set; } = new();
    
    [Description("Influence level of each person from 0.0 to 1.0 based on how others react to them")]
    public Dictionary<string, double> InfluenceScores { get; set; } = new();
    
    [Description("Social or professional groups identified like 'Senior staff', 'Junior engineers', 'Leadership team'")]
    public List<string> IdentifiedGroups { get; set; } = new();
    
    public class Person
    {
        [Description("Person's name as mentioned in the text")]
        public string Name { get; set; } = string.Empty;
        
        [Description("Their role or position that can be inferred like 'Department head', 'Engineer', 'Product manager'")]
        public string InferredRole { get; set; } = string.Empty;
        
        [Description("Observable characteristics or traits mentioned like 'Frustrated', 'Respected by juniors', 'Formal'")]
        public List<string> Characteristics { get; set; } = new();
        
        [Description("Their level in organization like 'Senior', 'Junior', 'Leadership', 'Executive'")]
        public string HierarchicalLevel { get; set; } = string.Empty;
    }
    
    public class Relationship
    {
        [Description("First person in the relationship")]
        public string Person1 { get; set; } = string.Empty;
        
        [Description("Second person in the relationship")]
        public string Person2 { get; set; } = string.Empty;
        
        [Description("Type of relationship like 'Professional', 'Hierarchical', 'Tension', 'Mentoring', 'Alliance'")]
        public string Type { get; set; } = string.Empty;
        
        [Description("Relationship strength from 0.0 to 1.0 based on observed interactions")]
        public double Strength { get; set; }
        
        [Description("Power direction: 'Bidirectional', 'Person1->Person2', 'Person2->Person1'")]
        public string Direction { get; set; } = string.Empty;
    }
}

public class ProductComparison
{
    [Description("Products being compared in the review")]
    public List<Product> Products { get; set; } = new();
    
    [Description("Main comparison categories extracted from the review like 'Performance', 'Cost', 'Usability' with relative scores")]
    public Dictionary<string, ComparisonDimension> Dimensions { get; set; } = new();
    
    [Description("Which product is recommended based on the review")]
    public string RecommendedProduct { get; set; } = string.Empty;
    
    [Description("Why this product is recommended, based on the reviewer's reasoning")]
    public string RecommendationRationale { get; set; } = string.Empty;
    
    public class Product
    {
        [Description("Product name as mentioned in the review")]
        public string Name { get; set; } = string.Empty;
        
        [Description("Rough satisfaction level from 0.0 to 1.0 - just make a reasonable guess based on positive/negative mentions")]
        public double OverallScore { get; set; } = 0.5;
        
        [Description("Scores for specific features mentioned like 'Speed: 0.8', 'Usability: 0.6'")]
        public Dictionary<string, double> FeatureScores { get; set; } = new();
        
        [Description("Positive aspects mentioned in the review")]
        public List<string> Strengths { get; set; } = new();
        
        [Description("Negative aspects or limitations mentioned")]
        public List<string> Weaknesses { get; set; } = new();
    }
    
    public class ComparisonDimension
    {
        [Description("Name of the comparison category like 'Performance', 'Cost', 'User Experience'")]
        public string Name { get; set; } = string.Empty;
        
        [Description("Relative performance score for each product in this category (0.0-1.0, where higher is better)")]
        public Dictionary<string, double> ProductScores { get; set; } = new();
        
        [Description("Which product performs best in this dimension")]
        public string Winner { get; set; } = string.Empty;
        
        [Description("How important this dimension seems to the reviewer from 0.0 to 1.0")]
        public double ImportanceWeight { get; set; }
    }
}

public class CognitiveProfile
{
    [Description("Observable thinking patterns like 'Analytical', 'Systematic', 'Iterative', 'Visual', based on their approach")]
    public List<string> ThinkingStyles { get; set; } = new();
    
    [Description("Specific behaviors observed during problem-solving like 'Drew diagrams', 'Asked clarifying questions', 'Sought team input'")]
    public List<string> ObservedBehaviors { get; set; } = new();
    
    [Description("Primary pattern for making decisions like 'Data-driven', 'Consensus-seeking', 'Analytical with validation'")]
    public string PrimaryDecisionMakingPattern { get; set; } = string.Empty;
    
    [Description("Strategies used to solve problems like 'Visual mapping', 'Iterative refinement', 'Breaking down complexity'")]
    public List<string> ProblemSolvingStrategies { get; set; } = new();
    
    [Description("Signs of self-awareness about thinking like 'Self-doubt expressions', 'Awareness of overthinking', 'Questioning own assumptions'")]
    public List<string> MetacognitiveIndicators { get; set; } = new();
    
    [Description("How they handle unclear situations like 'Seeks more data', 'Acknowledges uncertainty', 'Prefers validation before deciding'")]
    public string ApproachToUncertainty { get; set; } = string.Empty;
    
    [Description("Observable ways they express ideas during the session like 'Verbal reasoning', 'Uses analogies', 'Thinks out loud'")]
    public List<string> CommunicationPatterns { get; set; } = new();
    
    [Description("How they process information like 'Sequential step-by-step', 'Holistic big-picture', 'Comparative analysis'")]
    public string InformationProcessingStyle { get; set; } = string.Empty;
}

#endregion