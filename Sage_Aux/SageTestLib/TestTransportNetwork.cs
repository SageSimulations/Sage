/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Transport.Equipment;
using Highpoint.Sage.Transport.Management.Assignments;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Transport
{

    /// <summary>
    /// Summary description for zTestChemistry.
    /// </summary>
    [TestClass]
    public class TestTransportNetwork
    {
        private TransportSystem m_transportSystem;

        public TestTransportNetwork() { Initialize(); }

        [TestInitialize]
        public void Initialize()
        {
            int nEndpoints = 50;
            double interconnectedness = 0.2;

            Model model = new Model("Test Model");

            m_transportSystem = new TransportSystem("Test Transport System", Guid.NewGuid());
            model.AddService(m_transportSystem);

            TransportNetwork tn = new TransportNetwork(model, "Transport Network", Guid.NewGuid());
            Navigator nav = new Navigator(tn, new ConstantSpeedTravelTimeService(10.0));
            tn.SetNavigator(nav, 10.0);

            m_transportSystem.TransportNetwork = tn;
  
            CreateRandomTransportNetwork(model, nEndpoints, interconnectedness, m_transportSystem, 12345);

        }
        
        private void CreateRandomTransportNetwork(IModel model, int nEndpoints, double interconnectedness, TransportSystem transportSystem, int seed = 0)
        {
            Debug.Assert(transportSystem != null);
            TransportNetwork transportNetwork = transportSystem.TransportNetwork as TransportNetwork;
            Debug.Assert(transportNetwork != null);
            // Determine the X, Y and Z coordinates of the random endpoints.
            int xMin = 0;
            int xMax = 100;
            int yMin = 0;
            int yMax = 100;
            int zMin = 25;
            int zMax = 35;

            // Create 'nEndpoints' endpoints at random.
            Random r = new Random(seed);
            for (int i = 0; i < nEndpoints; i++)
            {
                double x = xMin + (r.NextDouble() * (xMax - xMin));
                double y = yMin + (r.NextDouble() * (yMax - yMin));
                double z = zMin + (r.NextDouble() * (zMax - zMin));
                BaseNode tpe = new BaseNode(model, string.Format("transNode {0:D3}", i), Guid.NewGuid(), i, x, y, z);
                #region Flotsam
                //tpe.InitializeIdentity(model, string.Format("endpoint %3d", i), null, Guid.NewGuid());
                //TimeSpan entryCost = TimeSpan.FromSeconds(10);
                //TimeSpan exitCost = TimeSpan.FromSeconds(10);
                //TimeSpan crossoverCost = TimeSpan.FromSeconds(90);
                //tpe.Initialize(model, string.Format("endpoint %3d", i), null, Guid.NewGuid(), entryCost, crossoverCost, exitCost);
                // tpe.RealCoordinates = new[] {3.1, 3.1, 2.2};
                #endregion
                transportNetwork.AddNode(tpe);
            }
            transportNetwork.Update();

            int segmentNum = 0;
            for (int i = 0; i < nEndpoints; i++)
            {
                BaseNode from = (BaseNode)transportNetwork.Nodes[i];
                for (int j = 0; j < nEndpoints; j++)
                {
                    if ((i != j) && r.NextDouble() < interconnectedness)
                    {
                        BaseNode to = (BaseNode)transportNetwork.Nodes[j];
                        BaseSegment segment = new BaseSegment(model, string.Format("Segment {0:D3}", segmentNum),
                            Guid.NewGuid(), segmentNum++, from, to)
                        {
                            IsSingleVehicle = true,
                            MinimumTravelSpacing = 5,
                            ExitControl = SegmentExitProtocol.Yield,
                            IsOneWay = true
                        };

                        #region Flotsam
                        //TimeSpan transitCost = TimeSpan.FromSeconds(100*r.NextDouble());
                        //tr.Initialize(from, to, transitCost);
                        #endregion
                        transportNetwork.AddSegment(segment);
                    }
                }
            }
            transportNetwork.Update();

        }

        [TestCleanup]
        public void destroy()
        {
            Trace.WriteLine("Done.");
        }


        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test checks the basic behavior of a transport system.")]
        public void TestTransportBasics()
        {
            IModel model = m_transportSystem.Model;
            ITransportNetwork transportNetwork = m_transportSystem.TransportNetwork;
            foreach (ISegment segment in m_transportSystem.TransportNetwork.Segments)
            {
                Console.WriteLine("{0}, {1:F1}, {2:F1}, {3:F1}, {4:F1}, {5:F1}, {6:F1}", segment, segment.FromNode.Coordinates.X, segment.FromNode.Coordinates.Y, segment.FromNode.Coordinates.Z, segment.ToNode.Coordinates.X, segment.ToNode.Coordinates.Y, segment.ToNode.Coordinates.Z);
            }

            INode from = transportNetwork.Nodes[0];
            INode to = transportNetwork.Nodes[17];

            VehicleType vt = new VehicleType(1,"Bobs car","Toyota","", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15),4,4, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
            Vehicle v = new Vehicle(model, "Vehicle", Guid.NewGuid(), 1, vt);
            List<ISegment> route = transportNetwork.Navigator.GetRoute(null, from, to, 3500.0);

            Console.WriteLine("Route is " + StringOperations.ToCommasAndAndedListOfNames(route));

            model.Starting += theModel =>
            {
                from.Accept(v);
                v.AddAssignment(new ShuttleAssignment(model, v, from, to, 3));
            };

            model.Start();
        }

        // ReSharper disable once FunctionComplexityOverflow
        //private void Populate(IModel model, IDataSet dataset)
        //{
        //    #region Set Basic Parameters 
        //    model.StartTime = dataset.SimulationParameters.StartTime;
        //    #endregion

        //    #region Create Transport Network.
        //    Dictionary<int, INode> nodesByNodeId = new Dictionary<int, INode>();
        //    foreach (ISegmentData segDat in dataset.TransportNetworkDataSource.SegmentData.Values)
        //    {
        //        if (!nodesByNodeId.ContainsKey(segDat.FromNodeId))
        //        {
        //            nodesByNodeId.Add(segDat.FromNodeId, new BaseNode(model, "Node " + segDat.FromNodeId, Guid.NewGuid(), segDat.FromNodeId, segDat.FromNodeX, segDat.FromNodeY, segDat.FromNodeZ));
        //        }
        //        if (!nodesByNodeId.ContainsKey(segDat.ToNodeId))
        //        {
        //            nodesByNodeId.Add(segDat.ToNodeId, new BaseNode(model, "Node " + segDat.ToNodeId, Guid.NewGuid(), segDat.ToNodeId, segDat.ToNodeX, segDat.ToNodeY, segDat.ToNodeZ));
        //        }
        //    }

        //    model.TransportNetwork = m_mdif.TransportNetworkFactory(model);
        //    Dictionary<int, BaseNode> nodesByLocationId = new Dictionary<int, BaseNode>();
        //    foreach (ILocationData locDat in dataset.TransportNetworkDataSource.LocationData.Values)
        //    {
        //        BaseNode node = (BaseNode)nodesByNodeId[locDat.NodeId];
        //        node.LocationType = locDat.Type;
        //        node.LocationSubType = locDat.SubType;
        //        node.LocationID = locDat.LocationId;
        //        node.LocationName = locDat.Name;
        //        nodesByLocationId.Add(locDat.LocationId, node);
        //    }

        //    foreach (ISegmentData segDat in dataset.TransportNetworkDataSource.SegmentData.Values)
        //    {

        //        BaseSegment segment = new BaseSegment(model, "Segment " + segDat.SegmentId, Guid.NewGuid(),
        //            segDat.SegmentId)
        //        {
        //            FromNode = nodesByNodeId[segDat.FromNodeId],
        //            ToNode = nodesByNodeId[segDat.ToNodeId],
        //            Grade = segDat.Grade,
        //            Length = segDat.Length
        //        };
        //        segment.FromNode.Outflows.Add(segment);
        //        segment.ToNode.Inflows.Add(segment);
        //        model.TransportNetwork.Segments.Add(segment);

        //        // Must create paired return segment for every (now unidirectional) segment.

        //        ISegment returnSegment = null;
        //        // TRAFFIC_RULE : One-way travel.
        //        if (!segment.IsOneWay)
        //        {
        //            returnSegment = new BaseSegment(model, "Segment " + (1000 + segDat.SegmentId),
        //                Guid.NewGuid(), 1000 + segDat.SegmentId)
        //            {
        //                FromNode = nodesByNodeId[segDat.ToNodeId],
        //                ToNode = nodesByNodeId[segDat.FromNodeId],
        //                Grade = -segDat.Grade,
        //                Length = segDat.Length,
        //                ReturnSegment = segment
        //            };

        //            segment.ReturnSegment = returnSegment;
        //            returnSegment.FromNode.Outflows.Add(returnSegment);
        //            returnSegment.ToNode.Inflows.Add(returnSegment);

        //            model.TransportNetwork.Segments.Add(returnSegment);
        //        }
        //        if (s_diagnostics) Console.WriteLine("Added outbound {0}{1}to the model.", segment.Name, segment.IsOneWay ? "" : string.Format("and return {0} ", returnSegment.Name));
        //    }

        //    List<INode> nodes = new List<INode>(nodesByNodeId.Values);
        //    ((TransportNetwork)model.TransportNetwork).SetListOfNodes(nodes);
        //    foreach (INode node in nodesByNodeId.Values)
        //    {
        //    }

        //    model.TransportNetwork.SetNavigator(m_mdif.TransportNetworkNavigatorFactory(model.TransportNetwork),
        //        dataset.SimulationParameters.TravelSpacing);

        //    #endregion

        //    #region Load materials.

        //    Dictionary<int, MaterialType> di = new Dictionary<int, MaterialType>();
        //    foreach (IMaterialTypesData imtd in dataset.MaterialTypesData.MaterialTypesData.Values)
        //    {
        //        MaterialType mt = new MaterialType(model, imtd.Name, Guid.NewGuid(), string.Empty, imtd.Purity);    // default purity is 1.0
        //        di.Add(imtd.UniqueId, mt);
        //    }

        //    foreach (var v in dataset.MaterialsData.MaterialData.Values)
        //    {
        //        MaterialType mt = di[v.MaterialTypeId];
        //        Material mat = new Material(RpmModel.UNG.GetNextName(mt.Name + " source #", 3), Guid.NewGuid(), mt, "", v.Quantity);
        //        BaseNode node = nodesByLocationId[v.MaterialLocationId];
        //        node.AddStock(mat);
        //        if (s_diagnostics) Console.WriteLine("Added {0} to {1}.", mat, node);
        //    }

        //    #endregion

        //    #region Create vehicles.

        //    IVehiclesDataSource ivds = dataset.VehiclesDataSource;
        //    Dictionary<int, IHaulUnitTypesData> haulUnitTypesDataDictionary = ivds.HaulUnitTypesData;
        //    Dictionary<int, IVehicleType> haulerTypesByTypeId = new Dictionary<int, IVehicleType>();
        //    foreach (var hutdEntry in haulUnitTypesDataDictionary)
        //    {
        //        TimeSpan loadTime = double.IsNaN(hutdEntry.Value.LoadTime)
        //            ? TimeSpan.MaxValue
        //            : TimeSpan.FromMinutes(hutdEntry.Value.LoadTime);
        //        VehicleType ht = new VehicleType(
        //            hutdEntry.Key,
        //            hutdEntry.Value.Manufacturer,
        //            hutdEntry.Value.Model,
        //            hutdEntry.Value.Description,
        //            loadTime,
        //            TimeSpan.FromMinutes(hutdEntry.Value.UnloadTime),
        //            hutdEntry.Value.Capacity,
        //            hutdEntry.Value.LoadingCapacity,
        //            TimeSpan.FromMinutes(hutdEntry.Value.LoadingCycleTime),
        //            TimeSpan.FromMinutes(hutdEntry.Value.StagingDuration),
        //            double.IsNaN(hutdEntry.Value.LoadTime) ? EquipmentRole.Hauler : EquipmentRole.Both);
        //        haulerTypesByTypeId.Add(hutdEntry.Key, ht);
        //    }

        //    Dictionary<int, IVehicle> haulUnitsByHaulerId = new Dictionary<int, IVehicle>();
        //    foreach (IHaulUnitsData ihud in ivds.HaulUnitsData.Values)
        //    {
        //        IVehicleType haulerType = haulerTypesByTypeId[ihud.HaulUnitTypeId];
        //        IVehicle haulUnit = new Vehicle(model, ihud.TruckName, Guid.NewGuid(), ihud.UniqueId, haulerType);
        //        haulUnitsByHaulerId.Add(haulUnit.TSID, haulUnit);

        //        BaseNode node = nodesByLocationId[ihud.StartLocationId];
        //        model.Starting += theModel =>
        //        {
        //            bool accepted = node.Accept(haulUnit);
        //            if (s_diagnostics)
        //                Console.WriteLine(
        //                    accepted
        //                        ? "{0} inserted into node {1} at {2}."
        //                        : "Load error - {0} not allowed into node {1} during setup.", haulUnit.Name, node,
        //                    theModel.Executive.Now);
        //        };
        //    }

        //    Dictionary<int, ILoadingUnitTypesData> loadingUnitTypesDataDictionary = ivds.LoadingUnitTypesData;
        //    Dictionary<int, IVehicleType> loaderTypesByTypeId = new Dictionary<int, IVehicleType>();
        //    foreach (var lutdEntry in loadingUnitTypesDataDictionary)
        //    {
        //        VehicleType lt = new VehicleType(
        //            lutdEntry.Key,
        //            lutdEntry.Value.Manufacturer,
        //            lutdEntry.Value.Model,
        //            lutdEntry.Value.Description,
        //            TimeSpan.MaxValue, // Load Time. It's a loader, and cannot, itself, be loaded.
        //            TimeSpan.MaxValue, // Unload Time. It's a loader, and cannot, itself, be unloaded.
        //            lutdEntry.Value.LoadingCapacity, // Capacity. What's in the bucket is all it can hold.
        //            lutdEntry.Value.LoadingCapacity,
        //            TimeSpan.FromMinutes(lutdEntry.Value.LoadingCycleTime),
        //            TimeSpan.MaxValue, // Staging Duration. It's a loader, and cannot, itself, be staged for loading.
        //            EquipmentRole.Loader
        //            );
        //        loaderTypesByTypeId.Add(lutdEntry.Key, lt);
        //    }

        //    Dictionary<int, IVehicle> loadingUnitsByLoaderId = new Dictionary<int, IVehicle>();
        //    foreach (ILoadingUnitsData ilud in ivds.LoadingUnitsData.Values)
        //    {
        //        IVehicleType loaderType = loaderTypesByTypeId[ilud.LoadingUnitTypeId];
        //        Vehicle loadingUnit = new Vehicle(model, ilud.Name, Guid.NewGuid(), ilud.UniqueId, loaderType);
        //        loadingUnitsByLoaderId.Add(loadingUnit.TSID, loadingUnit);
        //        BaseNode node = nodesByLocationId[ilud.StartLocationId];
        //        model.Starting += theModel =>
        //        {
        //            bool accepted = node.Accept(loadingUnit);
        //            if (s_diagnostics)
        //                Console.WriteLine(
        //                    accepted
        //                        ? "{0} inserted into node {1} at {2}."
        //                        : "Load error - {0} not allowed into node {1} during setup.", loadingUnit.Name, node,
        //                    theModel.Executive.Now);
        //        };
        //    }

        //    #endregion

        //    #region Now that we have vehicle type data, we can create transport data.

        //    TableDrivenTravelTimeService tdtts = new TableDrivenTravelTimeService();
        //    List<int> invalidTypeIdVals = new List<int>();
        //    foreach (ITravelTimeData ttd in dataset.TransportNetworkDataSource.TravelTimeData.Values)
        //    {
        //        if (!invalidTypeIdVals.Contains(ttd.HaulUnitTypeId))
        //        {
        //            IVehicleType ivt = null;
        //            IVehicleType ht;
        //            IVehicleType lut;

        //            if (haulerTypesByTypeId.TryGetValue(ttd.HaulUnitTypeId, out ht))
        //            {
        //                ivt = ht;
        //            }
        //            else if (loaderTypesByTypeId.TryGetValue(ttd.HaulUnitTypeId, out lut))
        //            {
        //                ivt = lut;
        //            }
        //            else
        //            {
        //                invalidTypeIdVals.Add(ttd.HaulUnitTypeId);
        //                if (s_diagnostics)
        //                    Console.WriteLine(
        //     "Travel times were provided for a vehicle type {0} which is not defined. This data will be ignored.",
        //     ttd.HaulUnitTypeId);
        //            }
        //            if (ivt != null)
        //            {
        //                ISegment seg = model.TransportNetwork.GetSegment(ttd.FromNodeId, ttd.ToNodeId);
        //                tdtts.AddTransitTimeData(seg, ivt, ttd.Empty, ttd.Full);
        //            }
        //        }
        //    }

        //    model.TransportNetwork.Navigator.SetTravelTimeService(tdtts);

        //    #endregion

        //    #region Create Tasks and assign them to Directors.

        //    #region Haulers' Tasks

        //    // Assign haulers.
        //    foreach (ILoadAndCarryTasksData lacTaskDat in dataset.LoadAndCarryTasksDataSource.LoadAndCarryTasksData.Values)
        //    {
        //        IVehicle hauler = haulUnitsByHaulerId[lacTaskDat.HaulUnitId];
        //        Trace.Assert(hauler != null,
        //            "Vehicle " + lacTaskDat.HaulUnitId + " specified for a LoadAndHaulTask, but it's not a hauler.");
        //        string name = "Haul" + hauler.Name;
        //        // ReSharper disable once UnusedVariable - creation registers it with the model.
        //        IMiningGoal miningGoal = new MiningGoal(model, name, Guid.NewGuid(), nodesByLocationId[lacTaskDat.SourceLocationId],
        //            nodesByLocationId[lacTaskDat.DestinationLocationId], 1234567, hauler);
        //    }

        //    #endregion

        //    #region Loaders' Tasks

        //    foreach (ITruckAndLoaderTasksData trLoTaDat in dataset.TruckAndLoaderTasksDataSource.TruckAndLoaderTasksData.Values)
        //    {
        //        IVehicle loader = loadingUnitsByLoaderId[trLoTaDat.LoadingUnitId];
        //        Debug.Assert(loader != null,
        //            "Vehicle " + trLoTaDat.LoadingUnitId + " specified for a TruckAndLoaderTask, but it's not a loader.");
        //        string name = "Load : " + loader.Name;
        //        // ReSharper disable once UnusedVariable - creation registers it with the model.
        //        IMiningGoal miningTask2 = new MiningGoal(model, name, Guid.NewGuid(), nodesByLocationId[trLoTaDat.SourceLocationId],
        //            nodesByLocationId[trLoTaDat.DestinationLocationId], 1234567, loader);
        //    }

        //    #endregion

        //    #region Planned Stoppages (i.e. Scheduled Breaks.)

        //    foreach (IPlannedStoppagesData ipsd in dataset.PlannedStoppagesDataSource.PlannedStoppagesData.Values)
        //    {
        //        string name = ipsd.Name;
        //        TravelInstructions travelInstructions = ipsd.TravelTo;
        //        double startTime = ipsd.StartTime;
        //        double duration = ipsd.Duration;
        //        double repeatPeriod = ipsd.RepeatPeriod;
        //        EquipmentRoleName equipmentTypeName = ipsd.EquipmentType;
        //        // ReSharper disable once UnusedVariable - creation registers it with the model.
        //        ScheduledBreakGoal pt = new ScheduledBreakGoal(model, name, Guid.NewGuid(), travelInstructions, startTime, duration,
        //            repeatPeriod, equipmentTypeName);
        //    }

        //    #endregion

        //    #region Unplanned Stoppages (i.e. Failures.)

        //    foreach (IUnplannedStoppagesData iupsd in dataset.UnplannedStoppagesDataSource.UnplannedStoppagesData.Values)
        //    {
        //        string name = iupsd.Name;
        //        IDoubleDistribution initialOffset = ToDoubleDistribution(model, "initialOffset", iupsd.FirstFailure);
        //        IDoubleDistribution mtbf = ToDoubleDistribution(model, "MTBF", iupsd.MeanTimeBetweenFailures);
        //        IDoubleDistribution mttr = ToDoubleDistribution(model, "MTTR", iupsd.MeanTimeToRepair);
        //        EquipmentRoleName equipmentTypeName = iupsd.EquipmentType;
        //        // TODO: TravelInstructions for certain types of failure tasks.
        //        // ReSharper disable once UnusedVariable - creation registers it with the model.
        //        FailureGoal ft = new FailureGoal(model, name, Guid.NewGuid(), initialOffset, mtbf, mttr, equipmentTypeName);
        //    }

        //    #endregion

        //    #endregion

        //}

    }

    public class ConstantSpeedTravelTimeService : ITravelTimeService
    {
        private readonly double m_speed;
        public ConstantSpeedTravelTimeService(double speed)
        {
            m_speed = speed;
        }

        public TimeSpan GetTransitDuration(ISegment segment, IVehicle vehicle)
        {
            return TimeSpan.FromSeconds(segment.Length * m_speed);
        }
    }
}
