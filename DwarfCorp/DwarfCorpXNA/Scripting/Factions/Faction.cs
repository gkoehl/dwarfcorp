// Faction.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DwarfCorp
{
    /// <summary>
    /// A faction is an independent collection of creatures, tied to an economy, rooms, and designations.
    /// Examples might be the player's dwarves, or the faction of goblins.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Faction
    {

        public Faction()
        {
            
        }

        public Faction(WorldManager world)
        {
            World = world;
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            TaskManager = new TaskManager();
            Stockpiles = new List<Stockpile>();
            DigDesignations = new Dictionary<ulong, BuildOrder>();
            GuardDesignations = new List<BuildOrder>();
            ChopDesignations = new List<Body>();
            AttackDesignations = new List<Body>();
            GatherDesignations = new List<Body>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            WrangleDesignations = new List<Body>();
            RoomBuilder = new RoomBuilder(this, world);
            WallBuilder = new PutDesignator(this, world);
            CraftBuilder = new CraftBuilder(this, world);
            IsRaceFaction = false;
            TradeMoney = 0.0m;
        }

        public Faction(OverworldFile.OverworldData.FactionDescriptor descriptor, Dictionary<string, Race> races )
        {
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            TaskManager = new TaskManager();
            Stockpiles = new List<Stockpile>();
            DigDesignations = new Dictionary<ulong, BuildOrder>();
            GuardDesignations = new List<BuildOrder>();
            ChopDesignations = new List<Body>();
            AttackDesignations = new List<Body>();
            GatherDesignations = new List<Body>();
            WrangleDesignations = new List<Body>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            IsRaceFaction = false;
            TradeMoney = 0.0m;
            PrimaryColor = descriptor.PrimaryColory;
            SecondaryColor = descriptor.SecondaryColor;
            Name = descriptor.Name;
            Race = races[descriptor.Race];
            Center = new Point(descriptor.CenterX, descriptor.CenterY);
        }

        public class WarParty : Expedition
        {
            public WarParty(DateTime date) : base(date)
            {
            }
        }

        public DwarfBux TradeMoney { get; set; }
        public Point StartingPlace { get; set; }
        public Point Center { get; set; }
        public int TerritorySize { get; set; }
        public Race Race { get; set; }
        public Economy Economy { get; set; }

        [JsonIgnore]
        public ComponentManager Components { get { return World.ComponentManager; }}

        public List<TradeEnvoy> TradeEnvoys { get; set; }
        public List<WarParty> WarParties { get; set; }
        public Dictionary<ulong, BuildOrder> DigDesignations { get; set; }
        public List<BuildOrder> GuardDesignations { get; set; }
        public List<Body> ChopDesignations { get; set; }
        public List<Body> AttackDesignations { get; set; }
        public List<Body> GatherDesignations { get; set; }
        public List<Stockpile> Stockpiles { get; set; }
        public List<CreatureAI> Minions { get; set; }
        public RoomBuilder RoomBuilder { get; set; }
        public PutDesignator WallBuilder { get; set; }
        public CraftBuilder CraftBuilder { get; set; }
        public Color DigDesignationColor { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }

        public TaskManager TaskManager { get; set; }
        public List<Creature> Threats { get; set; }

        public string Name { get; set; }
        public List<CreatureAI> SelectedMinions { get; set; }
        public bool IsRaceFaction { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        public List<Body> WrangleDesignations { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        public static List<CreatureAI> FilterMinionsWithCapability(List<CreatureAI> minions, GameMaster.ToolMode action)
        {
            return minions.Where(creature => creature.Stats.CurrentClass.HasAction(action)).ToList();
        }

        public void CollideMinions(DwarfTime time)
        {
            for (int i = 0; i < Minions.Count; i++)
            //{
            {
                CreatureAI minion = Minions[i];

                if (!minion.IsActive) continue;

                if (minion.Physics.CollideMode == Physics.CollisionMode.None)
                    continue;

                for (int j = i + 1; j < Minions.Count; j++)
                {
                    CreatureAI other = Minions[j];

                    if (!other.IsActive) continue;

                    // Grab both positions now to avoid the double lookup.
                    Vector3 otherPosition = other.Position;
                    Vector3 minionPosition = minion.Position;
                    Vector3 meToOther = otherPosition - minionPosition;

                    float dist = meToOther.Length();

                    if (!float.IsNaN(dist) && (dist < 0.25f))
                    {
                        other.Physics.ApplyForce(meToOther / (dist + 0.05f) * 50, (float)time.ElapsedGameTime.TotalSeconds);

                        // We need to set the physics on the second one as well.
                        Vector3 otherToMe = minionPosition - otherPosition;
                        minion.Physics.ApplyForce(otherToMe / (dist + 0.05f) * 50, (float)time.ElapsedGameTime.TotalSeconds);
                    }
                }
            }
        }


        public void Update(DwarfTime time)
        {
            RoomBuilder.Faction = this;
            CraftBuilder.Faction = this;
            WallBuilder.Faction = this;
            RoomBuilder.CheckRemovals();

            Minions.RemoveAll(m => m.IsDead);
            SelectedMinions.RemoveAll(m => m.IsDead);
            Minions.ForEach(m =>
            {
                m.Creature.SelectionCircle.IsVisible = false;
                m.Creature.Sprite.DrawSilhouette = false;
            });
            SelectedMinions.ForEach(m =>
            {
                m.Creature.SelectionCircle.IsVisible = true;
                m.Creature.Sprite.DrawSilhouette = true;
            });

            foreach (Room zone in GetRooms())
            {
                zone.ZoneBodies.RemoveAll(body => body.IsDead);
            }
            
            // Turned off until a non-O(n^2) collision method is create.
            //CollideMinions(time);

            List<ulong> removalKeys = new List<ulong>();
            foreach (KeyValuePair<ulong, BuildOrder> kvp in DigDesignations)
            {
                Voxel v = kvp.Value.Vox;;
                if (v.IsEmpty || v.Health <= 0.0f || v.Type.Name == "empty" || v.Type.IsInvincible)
                {
                    Drawer3D.UnHighlightVoxel(v);
                    removalKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < removalKeys.Count; i++)
            {
                DigDesignations.Remove(removalKeys[i]);
            }

            List<Body> gatherRemovals = (from b in GatherDesignations
                where b == null || b.IsDead
                select b).ToList();

            foreach(Body b in gatherRemovals)
            {
                GatherDesignations.Remove(b);
            }


            List<BuildOrder> removals = new List<BuildOrder>();
            foreach (BuildOrder d in GuardDesignations)
            {
                Voxel v = d.Vox;

                if (!v.IsEmpty && !(v.Health <= 0.0f) && v.Type.Name != "empty")
                {
                    continue;
                }

                removals.Add(d);

                if (!v.IsEmpty)
                {
                    v.Kill();
                }
            }

            foreach (BuildOrder v in removals)
            {
                GuardDesignations.Remove(v);
            }

            ChopDesignations.RemoveAll(tree => tree.IsDead);
            AttackDesignations.RemoveAll(body => body.IsDead);
            WrangleDesignations.RemoveAll(body => body.IsDead);
            foreach (var zone in RoomBuilder.DesignatedRooms)
            {
                zone.Update();
            }
            HandleThreats();
        }

        public bool IsTaskAssigned(Task task)
        {
            return Minions.Any(minion => minion.Tasks.Contains(task));
        }

        public CreatureAI GetNearestMinion(Vector3 location)
        {
            float closestDist = float.MaxValue;
            CreatureAI closestMinion = null;
            foreach (CreatureAI minion in Minions)
            {
                float dist = (minion.Position - location).LengthSquared();
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                closestMinion = minion;
            }

            return closestMinion;
        }

        public void HandleThreats()
        {
            List<Task> tasks = new List<Task>();
            List<Creature> threatsToRemove = new List<Creature>();
            foreach (Creature threat in Threats)
            {
                if (threat != null && !threat.IsDead)
                {
                    Task g = new KillEntityTask(threat.Physics, KillEntityTask.KillType.Auto);

                    if (!IsTaskAssigned(g))
                    {
                        if (!AttackDesignations.Contains(threat.Physics))
                        {
                            AttackDesignations.Add(threat.Physics);
                        }
                        tasks.Add(g);
                    }
                    else
                    {
                        threatsToRemove.Add(threat);
                    }
                }
                else
                {
                    threatsToRemove.Add(threat);
                }
            }

            foreach (Creature threat in threatsToRemove)
            {
               Threats.Remove(threat);
            }

            TaskManager.AssignTasks(tasks, Minions);
        }

        public void AssignGather(IEnumerable<Body> items)
        {
            List<Task> tasks = (from item in items where AddGatherDesignation(item) select new GatherItemTask(item) as Task).ToList();
            foreach (CreatureAI creature in Minions)
            {
                creature.Tasks.AddRange(tasks);
            }
        }

        public List<Room> GetRooms()
        {
            return RoomBuilder.DesignatedRooms;
        }

        public void OnVoxelDestroyed(Voxel v)
        {
            if(v.IsEmpty)
            {
                return;
            }

            Voxel Voxel = v;

            RoomBuilder.OnVoxelDestroyed(v);

            List<Stockpile> toRemove = new List<Stockpile>();
            List<Stockpile> currentStockpiles = new List<Stockpile>();
            currentStockpiles.AddRange(Stockpiles);
            foreach (Stockpile s in currentStockpiles)
            {
                if(s.ContainsVoxel(Voxel))
                {
                    s.RemoveVoxel(Voxel);
                }

                if(s.Voxels.Count == 0)
                {
                    toRemove.Add(s);
                }
            }

            foreach(Stockpile s in toRemove)
            {
                Stockpiles.Remove(s);
                s.Destroy();
            }
        }

        public int ComputeStockpileCapacity()
        {
            int space = 0;
            foreach (Stockpile pile in Stockpiles)
            {
                space += pile.Resources.MaxResources;
            }

            return space;
        }

        public int ComputeStockpileSpace()
        {
            return Stockpiles.Sum(pile => pile.Resources.MaxResources - pile.Resources.CurrentResourceCount);
        }

        public bool AddGatherDesignation(Body resource)
        {
            if (resource.Parent != Components.RootComponent || resource.IsDead)
            {
                return false;
            }

            if (GatherDesignations.Contains(resource)) return false;
            GatherDesignations.Add(resource);
            return true;
        }

        public BuildOrder GetClosestDigDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            BuildOrder closestVoxel = null;
            foreach(KeyValuePair<ulong, BuildOrder> kvp in DigDesignations)
            {
                Voxel vref = kvp.Value.Vox;
                Voxel v = vref;

                float d = (v.Position - position).LengthSquared();
                if(!(d < closestDist))
                {
                    continue;
                }

                closestDist = d;
                closestVoxel = kvp.Value;
            }

            return closestVoxel;
        }

        public BuildOrder GetClosestGuardDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            BuildOrder closestVoxel = null;
            foreach(BuildOrder designation in GuardDesignations)
            {
                Voxel vref = designation.Vox;
                Voxel v = vref;

                float d = (v.Position - position).LengthSquared();
                if(!(d < closestDist))
                {
                    continue;
                }

                closestDist = d;
                closestVoxel = designation;
            }

            return closestVoxel;
        }

        public BuildOrder GetGuardDesignation(Voxel vox)
        {
            return (from d in GuardDesignations
                let vref = d.Vox
                let v = vref
                where vox.Equals(v)
                select d).FirstOrDefault();
        }

        public BuildOrder GetDigDesignation(Voxel vox)
        {
            BuildOrder returnOrder;
            if (DigDesignations.TryGetValue(vox.QuickCompare, out returnOrder))
                return returnOrder;
            return new BuildOrder();
        }

        public void AddDigDesignation(BuildOrder order)
        {
            if (order.Vox == null) return;
            DigDesignations.Add(order.Vox.QuickCompare, order);
        }

        public void RemoveDigDesignation(Voxel vox)
        {
            if (DigDesignations.ContainsKey(vox.QuickCompare))
            {
                DigDesignations.Remove(vox.QuickCompare);
                Drawer3D.UnHighlightVoxel(vox);
            }
        }

        public bool IsDigDesignation(Voxel vox)
        {
            GamePerformance.Instance.TrackValueType<int>("Dig Designations", DigDesignations.Count);

            return DigDesignations.ContainsKey(vox.QuickCompare);
            /*
            for (int i = 0; i < DigDesignations.Count; i++)
            {
                if (DigDesignations[i].Vox.IsSameAs(vox)) return true;
            }
            return false;
             * */
        }


        public bool IsGuardDesignation(Voxel vox)
        {
            return GuardDesignations.Select(d => d.Vox).Select(vref => vref).Any(vox.Equals);
        }

        public bool AddResources(ResourceAmount resources)
        {
            ResourceAmount amount = new ResourceAmount(resources.ResourceType, resources.NumResources);
            foreach (Stockpile stockpile in Stockpiles)
            {
                int space = stockpile.Resources.MaxResources - stockpile.Resources.CurrentResourceCount;

                if(space >= amount.NumResources)
                {
                    stockpile.Resources.AddResource(amount);
                    stockpile.HandleBoxes();
                    return true;
                }
                else
                {
                    stockpile.Resources.AddResource(amount);
                    amount.NumResources -= space;
                    stockpile.HandleBoxes();
                    if(amount.NumResources == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Room GetNearestRoomOfType(string typeName, Vector3 position)
        {
            List<Room> rooms = GetRooms();
            Room desiredRoom = null;
            float nearestDistance = float.MaxValue;

            foreach (Room room in rooms)
            {
                if (room.RoomData.Name != typeName) continue;
                float dist =
                    (room.GetNearestVoxel(position).Position - position).LengthSquared();

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    desiredRoom = room;
                }
            }


            return desiredRoom;
        }


        public Stockpile GetNearestStockpile(Vector3 position, Func<Stockpile, bool> predicate )
        {
            Stockpile nearest = null;

            float closestDist = float.MaxValue;
            foreach(Stockpile stockpile in Stockpiles.Where(predicate))
            {
                float dist = (stockpile.GetBoundingBox().Center() - position).LengthSquared();

                if(dist < closestDist)
                {
                    closestDist = dist;
                    nearest = stockpile;
                }
            }

            return nearest;
        }


        public Stockpile GetIntersectingStockpile(Voxel v)
        {
            return Stockpiles.FirstOrDefault(pile => pile.Intersects(v));
        }

        public Stockpile GetIntersectingStockpile(BoundingBox v)
        {
            return Stockpiles.FirstOrDefault(pile => pile.Intersects(v));
        }

        public List<Stockpile> GetIntersectingStockpiles(BoundingBox v)
        {
            return Stockpiles.Where(pile => pile.Intersects(v)).ToList();
        }

        public List<Room> GetIntersectingRooms(BoundingBox v)
        {
            return RoomBuilder.DesignatedRooms.Where(room => room.Intersects(v)).ToList();
        }

        public bool IsInStockpile(Voxel v)
        {
            Voxel vRef = v;
            return Stockpiles.Any(s => s.ContainsVoxel(vRef));
        }

        public Body GetRandomGatherDesignationWithTag(string tag)
        {
            List<Body> des = GatherDesignations.Where(c => c.Tags.Contains(tag)).ToList();
            return des.Count == 0 ? null : des[MathFunctions.Random.Next(0, des.Count)];
        }


        public bool HasFreeStockpile()
        {
            return Stockpiles.Any(s => !s.IsFull());
        }

        public bool HasFreeStockpile(ResourceAmount toPut)
        {
            return Stockpiles.Any(s => !s.IsFull() && s.IsAllowed(toPut.ResourceType));
        }

        public Body FindNearestItemWithTags(string tag, Vector3 location, bool filterReserved)
        {
            Body closestItem = null;
            float closestDist = float.MaxValue;
            List<Zone> zones = new List<Zone>();
            zones.AddRange(RoomBuilder.DesignatedRooms.Where(room => room != null && room.IsBuilt));
            zones.AddRange(Stockpiles);

            foreach (Zone s in zones)
            {
                
                Body i = s.GetNearestBodyWithTag(location, tag, filterReserved);

                if (i != null)
                {
                    float d = (i.GlobalTransform.Translation - location).LengthSquared();
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestItem = i;
                    }
                }
            }

            

            return closestItem;
        }

        public int CompareZones(Zone a, Zone b, Vector3 pos)
        {
            if(a == b) 
            {
                return 0;
            }
            else
            {
               
                float costA = (pos- a.GetBoundingBox().Center()).LengthSquared();
                float costB = (pos - b.GetBoundingBox().Center()).LengthSquared();

                if(costA < costB)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        public Dictionary<string, ResourceAmount> ListResources()
        {
            Dictionary<string, ResourceAmount> toReturn = new Dictionary<string, ResourceAmount>();

            foreach(Stockpile stockpile in Stockpiles)
            {
                foreach (ResourceAmount resource in stockpile.Resources)
                {
                    if (resource.NumResources == 0)
                    {
                        continue;
                    }

                    if (toReturn.ContainsKey(resource.ResourceType))
                    {
                        toReturn[resource.ResourceType].NumResources += resource.NumResources;
                    }
                    else
                    {
                        toReturn[resource.ResourceType] = new ResourceAmount(resource);
                    }
                }
            }

            return toReturn;
        }

        public List<ResourceAmount> GetResourcesWithTags(List<Quantitiy<Resource.ResourceTags>> tags)
        {
            Dictionary<Resource.ResourceTags, int> tagsRequired = new Dictionary<Resource.ResourceTags, int>();
            Dictionary<Resource.ResourceTags, int> tagsGot = new Dictionary<Resource.ResourceTags, int>();
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> amounts = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();

            foreach (Quantitiy<Resource.ResourceTags> quantity in tags)
            {
                tagsRequired[quantity.ResourceType] = quantity.NumResources;
                tagsGot[quantity.ResourceType] = 0;
            }

            Random r = new Random();

            foreach (Stockpile stockpile in Stockpiles)
            {
                foreach (ResourceAmount resource in stockpile.Resources.OrderBy(x => r.Next()))
                {
                    foreach (var requirement in tagsRequired)
                    {
                        int got = tagsGot[requirement.Key];

                        if (requirement.Value <= got) continue;

                        if (!ResourceLibrary.GetResourceByName(resource.ResourceType).Tags.Contains(requirement.Key)) continue;

                        int amountToRemove = System.Math.Min(resource.NumResources, requirement.Value - got);

                        if (amountToRemove <= 0) continue;

                        tagsGot[requirement.Key] += amountToRemove;

                        if (amounts.ContainsKey(resource.ResourceType))
                        {
                            amounts[resource.ResourceType].NumResources += amountToRemove;
                        }
                        else
                        {
                            amounts[resource.ResourceType] = new ResourceAmount(resource.ResourceType, amountToRemove);
                        }
                    }
                }
            }

            return amounts.Values.ToList();
        }

        public bool HasResources(IEnumerable<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach (Quantitiy<Resource.ResourceTags> resource in resources)
            {
                int count = Stockpiles.Sum(stock => stock.Resources.GetResourceCount(resource.ResourceType));

                if (count < resource.NumResources)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasResources(IEnumerable<ResourceAmount> resources)
        {
            foreach (ResourceAmount resource in resources)
            {
                int count = Stockpiles.Sum(stock => stock.Resources.GetResourceCount(resource.ResourceType));

                if(count < resource.NumResources)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasResources(ResourceLibrary.ResourceType resource)
        {
            return HasResources(new List<ResourceAmount>() {new ResourceAmount(resource)});
        }

        public bool RemoveResources(List<ResourceAmount> resources, Vector3 position, bool createItems = true)
        {
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> amounts = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();

            foreach (ResourceAmount resource in resources)
            {
                if (!amounts.ContainsKey(resource.ResourceType))
                {
                    amounts.Add(resource.ResourceType, new ResourceAmount(resource));
                }
                else
                {
                    amounts[resource.ResourceType].NumResources += resource.NumResources;
                }
            }

            if (!HasResources(amounts.Values))
            {
                return false;
            }


            List<Stockpile> stockpilesCopy = new List<Stockpile>(Stockpiles);
            stockpilesCopy.Sort((a, b) => CompareZones(a, b, position));


            foreach(ResourceAmount resource in resources)
            {
                int count = 0;
                List<Vector3> positions = new List<Vector3>();
                foreach (Stockpile stock in stockpilesCopy)
                {
                    int num =  stock.Resources.RemoveMaxResources(resource, resource.NumResources - count);
                    stock.HandleBoxes();
                    if(stock.Boxes.Count > 0)
                    {
                        for(int i = 0; i < num; i++)
                        {
                            positions.Add(stock.Boxes[stock.Boxes.Count - 1].LocalTransform.Translation);
                        }
                    }

                    count += num;

                    if(count >= resource.NumResources)
                    {
                        break;
                    }

                }

                if (createItems)
                {
                    foreach (Vector3 vec in positions)
                    {
                        Body newEntity =
                            EntityFactory.CreateEntity<Body>(resource.ResourceType + " Resource",
                                vec + MathFunctions.RandVector3Cube()*0.5f);

                        TossMotion toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f),
                            2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                        newEntity.GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                        newEntity.AnimationQueue.Add(toss);
                        toss.OnComplete += () => toss_OnComplete(newEntity);

                    }
                }

            }

            return true;
        }

        void toss_OnComplete(Body entity)
        {
            entity.Die();
            
        }

        public void Hire(Applicant currentApplicant)
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomData.Name == "BalloonPort").ToList();

            if (rooms.Count == 0)
            {
                return;
            }

            Economy.CurrentMoney -= currentApplicant.Level.Pay*4m;
            Dwarf newMinion =
                EntityFactory.GenerateDwarf(
                    rooms.First().GetBoundingBox().Center() + Vector3.UnitY * 15,
                    Components, GameState.Game.Content, GameState.Game.GraphicsDevice, World.ChunkManager,
                    World.Camera, this, World.PlanService, "Player", currentApplicant.Class, currentApplicant.Level.Index).GetChildrenOfType<Dwarf>().First();

            newMinion.Stats.CurrentClass = currentApplicant.Class;
            newMinion.Stats.LevelIndex = currentApplicant.Level.Index - 1;
            newMinion.Stats.LevelUp();
            newMinion.Stats.FullName = currentApplicant.Name;
            newMinion.AI.AddMoney(currentApplicant.Level.Pay * 4m);
            newMinion.AI.Biography = currentApplicant.Biography;

            World.MakeAnnouncement(String.Format("{0} was hired as a {1}.",
                currentApplicant.Name, currentApplicant.Level.Name), newMinion.AI.ZoomToMe,  ContentPaths.Audio.Oscar.sfx_gui_positive_generic);
        }

        public Body DispatchBalloon()
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomData.Name == "BalloonPort").ToList();

            if (rooms.Count == 0)
            {
                return null;
            }

            Vector3 pos = rooms.First().GetBoundingBox().Center();
            return  EntityFactory.CreateBalloon(pos + new Vector3(0, 1000, 0), pos + Vector3.UnitY * 15, Components, GameState.Game.Content, GameState.Game.GraphicsDevice, new ShipmentOrder(0, null), this);
        }

        public List<Body> GenerateRandomSpawn(int numCreatures, Vector3 position)
        {
            if (Race.CreatureTypes.Count == 0)
            {
                return new List<Body>();
            }

            List<Body> toReturn = new List<Body>();
            for (int i = 0; i < numCreatures; i++)
            {
                string creature = Race.CreatureTypes[MathFunctions.Random.Next(Race.CreatureTypes.Count)];
                Vector3 offset = MathFunctions.RandVector3Cube() * 2;
                Voxel voxel = new Voxel();
                
                if (World.ChunkManager.ChunkData.GetFirstVoxelUnder(position + offset, ref voxel, true))
                {
                    Body body = EntityFactory.CreateEntity<Body>(creature, voxel.Position + new Vector3(0.5f, 1, 0.5f));
                    CreatureAI ai = body.GetChildrenOfType<CreatureAI>().FirstOrDefault();
                    
                    if (ai != null)
                    {
                        ai.Faction.Minions.Remove(ai);
                        
                        Minions.Add(ai);
                        ai.Faction = this;
                        ai.Creature.Allies = Name;
                    }

                    toReturn.Add(body);
                }
            }

            return toReturn;
        }

        public int CountResourcesWithTag(Resource.ResourceTags tag)
        {
            List<ResourceAmount> resources = ListResourcesWithTag(tag);
            int amounts = 0;

            foreach (ResourceAmount amount in resources)
            {
                amounts += amount.NumResources;
            }

            return amounts;
        }

        public List<ResourceAmount> ListResourcesWithTag(Resource.ResourceTags tag)
        {
            Dictionary<string, ResourceAmount> resources = ListResources();
            return (from pair in resources where ResourceLibrary.GetResourceByName(pair.Value.ResourceType).Tags.Contains(tag) select pair.Value).ToList();
        }

        public Room GetNearestRoom(Vector3 position)
        {
            List<Room> rooms = GetRooms();
            Room desiredRoom = null;
            float nearestDistance = float.MaxValue;

            foreach (Room room in rooms)
            {
                if (room.Voxels.Count == 0) continue;
                float dist =
                    (room.GetNearestVoxel(position).Position - position).LengthSquared();

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    desiredRoom = room;
                }
            }


            return desiredRoom;
        }

        public void AddMinion(CreatureAI minion)
        {
            Minions.Add(minion);
            Inventory targetInventory = minion.GetComponent<Inventory>();

            if (targetInventory != null)
            {
                targetInventory.OnDeath += resources =>
                {
                    if (resources == null) return;
                    AssignGather(resources);
                };
            }
        }
    }

}
