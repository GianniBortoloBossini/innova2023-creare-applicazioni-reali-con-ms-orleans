﻿using Orleans.UrlShortner.Grains;
using Orleans.UrlShortner.StatelessWorkers;
using Orleans.UrlShortner.Tests.Fixtures;

namespace Orleans.UrlShortner.Tests.Grains;
internal class UrlShortnerStatisticsGrainTests
{
    private ClusterFixture fixture;

    [SetUp]
    public void SetUp()
    {
        this.fixture = new ClusterFixture();
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(100)]
    public async Task GetTotal_Should_Count_New_Registrations(int registrationTimes)
    {
        // ARRANGE
        var statisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
        
        List<Task> tasks = new();
        for (int i = 0; i < registrationTimes; i++)
            tasks.Add(statisticsGrain.RegisterNew());

        await Task.WhenAll(tasks);

        // ACT

        /* PERCHE' QUESTO DELAY? 
         * Provare a decommentare gli attributi OneWay e ReadOnly sull'interfaccia del grano e scoprirai che i test falliscono.
         * E' legato alle migliorie alle performance introdotte da Orleans nella gestione del multi-threading in presenza di quei due attributi:
         * await non attende che il Task sia effettivamente terminato! 
         * Per questo è necessario attendere 
         */
        await Task.Delay(100); 

        var result = await statisticsGrain.GetTotal();

        // ASSERT
        Assert.That(result, Is.EqualTo(registrationTimes));
    }

    [TestCase(3, 3100)]
    [TestCase(60, 61000)]
    public async Task GetNumberOfActiveShortenedRouteSegment_Should_Count_Active_Shortened_Route_Segment(int validFor, int checkAfter)
    {
        // ARRANGE
        var applicationStatisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");
        await applicationStatisticsGrain.Initialize();

        var domainWithoutStatistics = (new Uri("https://www.codiceplastico.com/")).Host;
        var domainWithoutStatisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IDomainGrain>(domainWithoutStatistics);
        await domainWithoutStatisticsGrain.Initialize();

        var url = "https://capitalecultura2023.it/";
        var domain = (new Uri(url)).Host;
        var domainStatisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IDomainGrain>(domain);
        await domainStatisticsGrain.Initialize();

        var shortenerRouteSegmentWorker = fixture.Cluster.GrainFactory.GetGrain<IShortenedRouteSegmentStatelessWorker>(0);
        var shortenerRouteSegment = await shortenerRouteSegmentWorker.Create(url);

        var urlshortenerGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortenerGrain>(shortenerRouteSegment);

        // ACT
        await urlshortenerGrain.CreateShortUrl(url, false, 3);

        var statisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");

        await Task.Delay(100);

        var applicationTotalActivation = await applicationStatisticsGrain.GetTotal();
        var applicationNumberOfActiveShortenerRouteSegment = await applicationStatisticsGrain.GetNumberOfActiveShortenedRouteSegment();
        var domainTotalActivation = await domainStatisticsGrain.GetTotal();
        var domainNumberOfActiveShortenerRouteSegment = await domainStatisticsGrain.GetNumberOfActiveShortenedRouteSegment();
        var domainWithoutStatisticsTotalActivation = await domainWithoutStatisticsGrain.GetTotal();
        var domainWithoutStatisticsNumberOfActiveShortenerRouteSegment = await domainWithoutStatisticsGrain.GetNumberOfActiveShortenedRouteSegment();

        // ASSERT
        Assert.That(applicationTotalActivation, Is.EqualTo(1));
        Assert.That(applicationNumberOfActiveShortenerRouteSegment, Is.EqualTo(1));
        Assert.That(domainTotalActivation, Is.EqualTo(1));
        Assert.That(domainNumberOfActiveShortenerRouteSegment, Is.EqualTo(1));
        Assert.That(domainWithoutStatisticsTotalActivation, Is.EqualTo(0));
        Assert.That(domainWithoutStatisticsNumberOfActiveShortenerRouteSegment, Is.EqualTo(0));

        await Task.Delay(3100);

        totalActivation = await statisticsGrain.GetTotal();
        numberOfActiveShortenerRouteSegment = await statisticsGrain.GetNumberOfActiveShortenedRouteSegment();

        Assert.That(totalActivation, Is.EqualTo(1));
        Assert.That(numberOfActiveShortenerRouteSegment, Is.EqualTo(0));
    }

    [Test]
    public async Task GetNumberOfActiveShortenedRouteSegment_Should_Count_Active_Shortened_Route_Segment_With_Reminder()
    {
        var url = "https://capitalecultura2023.it/";
        var shortenerRouteSegmentWorker = fixture.Cluster.GrainFactory.GetGrain<IShortenedRouteSegmentStatelessWorker>(0);
        var shortenerRouteSegment = await shortenerRouteSegmentWorker.Create(url);

        var urlshortenerGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortenerGrain>(shortenerRouteSegment);
        await urlshortenerGrain.CreateShortUrl(url, false, 60);

        var statisticsGrain = fixture.Cluster.GrainFactory.GetGrain<IUrlShortnerStatisticsGrain>("url_shortner_statistics");

        await Task.Delay(100);

        var totalActivation = await statisticsGrain.GetTotal();
        var numberOfActiveShortenerRouteSegment = await statisticsGrain.GetNumberOfActiveShortenedRouteSegment();

        Assert.That(totalActivation, Is.EqualTo(1));
        Assert.That(numberOfActiveShortenerRouteSegment, Is.EqualTo(1));

        await Task.Delay(61000);

        applicationTotalActivation = await applicationStatisticsGrain.GetTotal();
        applicationNumberOfActiveShortenerRouteSegment = await applicationStatisticsGrain.GetNumberOfActiveShortenedRouteSegment();
        domainTotalActivation = await domainStatisticsGrain.GetTotal();
        domainNumberOfActiveShortenerRouteSegment = await domainStatisticsGrain.GetNumberOfActiveShortenedRouteSegment();
        domainWithoutStatisticsTotalActivation = await domainWithoutStatisticsGrain.GetTotal();
        domainWithoutStatisticsNumberOfActiveShortenerRouteSegment = await domainWithoutStatisticsGrain.GetNumberOfActiveShortenedRouteSegment();

        Assert.That(applicationTotalActivation, Is.EqualTo(1));
        Assert.That(applicationNumberOfActiveShortenerRouteSegment, Is.EqualTo(0));
        Assert.That(domainTotalActivation, Is.EqualTo(1));
        Assert.That(domainNumberOfActiveShortenerRouteSegment, Is.EqualTo(0));
        Assert.That(domainWithoutStatisticsTotalActivation, Is.EqualTo(0));
        Assert.That(domainWithoutStatisticsNumberOfActiveShortenerRouteSegment, Is.EqualTo(0));
    }
}
