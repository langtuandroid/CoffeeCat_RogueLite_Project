﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using CoffeeCat;
using CoffeeCat.Utils;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

namespace RandomDungeonWithBluePrint
{
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    [SuppressMessage("ReSharper", "HeapView.ClosureAllocation")]
    [SuppressMessage("ReSharper", "HeapView.DelegateAllocation")]
    public static class FieldBuilder
    {
        public static Field Build(FieldBluePrint bluePrint)
        {
            // bluePrint 스크립터블 오브젝트에 따라 Field를 생성
            var field = new Field
            {
                Size = bluePrint.Size,
                MaxRoomNum = Random.Range(bluePrint.MinRoomNum, bluePrint.MaxRoomNum + 1),
                Gates = new List<Gate>()
            };

            MakeSection(field, bluePrint);　     
            MakeRooms(field);                   
            MakeRoomsType(field, bluePrint);    
            MakeBranches(field, bluePrint);     
            field.BuildGrid();                  
            return field;
        }

        private static void MakeSection(Field field, FieldBluePrint bluePrint)
        {
            field.Sections = bluePrint.Sections.Select(s => new Section(s)).ToList();
        }

        private static void MakeRooms(Field field)
        {
            MakeIndispensableRooms(field); // 필수적인 Room 생성
            MakeStochasticRooms(field);    // 확률적인 Room 생성
            MakeRelay(field);              // Room이 없는 Section에 Branch의 연결지점 생성 
        }

        // 필수적인 Room 생성
        private static void MakeIndispensableRooms(Field field)
        {
            var targetSections = field.Sections.Where(s => s.RoomIndispensable)
                .OrderBy(s => s.Index)
                .Take(field.MaxRoomNum);
            foreach (var section in targetSections)
            {
                MakeRoom(section);
            } 
        }

        // 필수가 아닌 Room (확률적으로) 생성
        private static void MakeStochasticRooms(Field field)
        {
            // !RoomIsFull : 방의 개수가 한계가 아닐 때
            // ExistRoomToBeMake : 방을 만들 장소가 존재할 때
            while (!field.RoomIsFull && field.ExistRoomToBeMake)
            {
                // 가중치에 따라 랜덤으로 섹션을 정해서 섹션에 Room 을 생성
                var targetSection = RaffleForMakeRoom(field);
                MakeRoom(targetSection);
            }
        }

        private static void MakeRoom(Section section)
        {
            var sectionWithPadding = section.Rect.AddPadding(2); // Padding을 적용한 새로운 RectInt
            var roomRect = GetRoomRect(sectionWithPadding, section.MinRoomSize); // MinRoomsize ~ sectionWithPadding 까지의 랜덤 사이즈로 방 사이즈를 결정
            var safeArea = sectionWithPadding.SafeAreaOfInclusion(roomRect); //sectionWithPadding의 가로 세로 값에서 roomRect의 가로 세로 값을 뺀 후 room이 생성될 수 있는 safeArea 정의
            roomRect.x = Random.Range(safeArea.xMin, safeArea.xMax);
            roomRect.y = Random.Range(safeArea.yMin, safeArea.yMax); // SafeArea의 xMax, yMax에 생성되더라도 sectionWithPadding 내에 생성됨
            section.Room = new Room(roomRect);

            // var mod = Random.Range(0, 10) % 2;
            foreach (var direction in Constants.Direction.FourDirections)
            {
                // var edgePositions = section.Room.Edge[direction].ToList(); // Room 의 방향별 가장자리
                
                section.Room.SetJoint(direction, section.Room.EdgeWithCenter[direction]);

                // for (var i = 0; i < edgePositions.Count; i++)
                // {
                //     if (i % 2 == mod)
                //     {
                //         // 가장자리 변에서 하나씩 점프하며 출구 예정지를 정의
                //         section.Room.SetJoint(direction, edgePositions[i]);
                //     }
                // }
            }
            
            // 정의된 Joint에 따라 Wall Tiles에서 Joint Position 제외
            section.Room.ExceptJointTilesInWallDictionary();
        }

        private static RectInt GetRoomRect(RectInt source, Vector2Int minSize)
        {
            if (minSize.x < 0 || minSize.y < 0 || source.width < minSize.x || source.height < minSize.y)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new RectInt(source.x, source.y, Random.Range(minSize.x, source.width + 1), Random.Range(minSize.y, source.height + 1));
        }

        private static void MakeRelay(Field field)
        {
            // Room이 존재하지 않는 Section
            foreach (var section in field.Sections.Where(s => !s.ExistRoom))
            {
                var padding = section.Rect.AddPadding(2);
                var point = new Vector2Int(Random.Range(padding.xMin, padding.xMax), Random.Range(padding.yMin, padding.yMax));
                section.Relay = new Relay(section.Index, point);
            }
        }

        // Room 을 생성하기 위한 추첨
        private static Section RaffleForMakeRoom(Field field)
        {
            var candidate = field.Sections.Where(s => !s.ExistRoom).OrderBy(s => s.Index).ToList(); // candidate : 후보
            var rand = Random.Range(0, candidate.Sum(c => c.MakeRoomWeight));
            var pick = 0;
            for (var i = 0; i < candidate.Count; i++)
            {
                if (rand < candidate[i].MakeRoomWeight)
                {
                    pick = i;
                    break;
                }

                rand -= candidate[i].MakeRoomWeight;
            }

            return candidate[pick];
        }

        private static void MakeBranches(Field field, FieldBluePrint bluePrint)
        {
            field.Connections = bluePrint.Connections.Select(c => new Connection { From = c.From, To = c.To }).ToList();    
            
            // bluePrint에 정의한 Connections가 하나도 존재하지 않음 && 초기 커넥션 자동생성 여부 true
            if (!field.Connections.Any() && bluePrint.AutoGenerateDefaultConnections)
            {
                ExtendConnections(field);
            }
            
            // BluePrint.Connections에 정의되어있지 않아도 모든 Sections의 연결을 정의
            // Connection을 보완하다
            ComplementAllConnection(field);
            
            MakeAdditionalBranch(field, bluePrint);
            
            // BluePrint의 Section중 Room이 존재하는 Section만 Connection을 정의
            if (bluePrint.IsConnectionOnlyExistRoom)
            {
                var roomExistIndex =
                    field.Sections.Where(section => section.ExistRoom)
                         .Select(section => section.Index)
                         .ToArray();
                field.Connections = field.Connections.Where(connection =>
                                                                    roomExistIndex.Contains(connection.From) &&
                                                                    roomExistIndex.Contains(connection.To))
                                             .Select(c => new Connection() { From = c.From, To = c.To })
                                             .ToList();
                
                // From 과 To 중에 하나만 Room이 존재하지 않는 경우만 지워버리면 되지 않을까?
            }


            field.Branches = field.Connections.SelectMany(c => Join(field.GetSection(c.From), field.GetSection(c.To), field.Gates)).ToList();
            
            // MakeBranches내부에서 Joint의 Connected 변수를 변경하여 최종 연결상태를 정의하기 때문에 여기서 호출
            field.Rooms.ForEach(room => room.ExceptJointTilesInWallDictionary());
        }

        private static void ExtendConnections(Field field)
        {
            var targetSection = field.Sections[Random.Range(0, field.Sections.Count)];
            while (targetSection != null)
            {
                // targetSection과 인접하면서 Connection이 없는 Section을 찾음
                var isolatedSections = field.Sections.Where(s => s != targetSection && targetSection.AdjoinWith(s) &&
                                                        field.IsIsolatedSection(s)).ToList();
                // 결과가 없으면 loop를 중단
                if (!isolatedSections.Any())
                {
                    break;
                }

                var nextSection = isolatedSections[Random.Range(0, isolatedSections.Count)];

                // targetSection과 nextSection을 연결하는 Connection을 생성
                field.Connections.Add(new Connection { From = targetSection.Index, To = nextSection.Index });

                // nextSection을 타겟으로 변경
                targetSection = nextSection;
            }

            var sections = field.Sections
                                .Where(s => field.IsIsolatedSection(s) && !field.ExistConnectedSectionAround(s))
                                .ToList();

            // Connection이 없는 Section이면서 / 인접한 Section들도 모두 Connection이 없는 경우
            // Section이 고립되지 않도록
            while(sections.Any())
            {
                var isolatedSection = sections[Random.Range(0, sections.Count)];
                var fromSection = field.GetSectionsAdjoinWith(isolatedSection).FirstOrDefault();
                field.Connections.Add(new Connection { From = fromSection.Index, To = isolatedSection.Index });
                
                sections = field.Sections
                                .Where(s => field.IsIsolatedSection(s) && !field.ExistConnectedSectionAround(s))
                                .ToList();
            }
        }

        private static void ComplementAllConnection(Field field)
        {
            var resultSections = field.IsolatedAndExistConnectedSectionAroundSections().ToList();
            while (resultSections.Any())
            {
                var isolatedSection = resultSections[Random.Range(0, resultSections.Count)];

                // 인접한 Section 중에서 길이 이어져 있는 Section
                var fromSection = GetAdjoinedSection(field, isolatedSection);
                field.Connections.Add(new Connection { From = fromSection.Index, To = isolatedSection.Index });

                resultSections.Remove(isolatedSection);
            }
        }

        private static void MakeAdditionalBranch(Field field, FieldBluePrint bluePrint)
        {
            var randomBranchNum = Random.Range(bluePrint.MinRandomBranchNum, bluePrint.MaxRandomBranchNum + 1);
            for (var i = 0; i < randomBranchNum; i++)
            {
                var roomContainingSections = field.Sections.Where(s => s.ExistRoom).ToList();
                if (!roomContainingSections.Any())
                {
                    break;
                }
                
                var targetSection = roomContainingSections[Random.Range(0, roomContainingSections.Count)];

                var unconnectedSections = field.GetSectionsAdjoinWith(targetSection)
                                               .Where(s => !field.Connected(s, targetSection)).ToList();
                var pairSection = unconnectedSections.Count > 0 ? unconnectedSections[Random.Range(0, unconnectedSections.Count)] : null;
                if (pairSection == null)
                {
                    break;
                }

                field.Connections.Add(new Connection { From = targetSection.Index, To = pairSection.Index });
            }
        }

        private static Section GetAdjoinedSection(Field field, Section target)
        {
            var adjoinedSections = field.GetSectionsAdjoinWithRoute(target).ToList();
            return adjoinedSections[Random.Range(0, adjoinedSections.Count)];
        }

        private static IEnumerable<Vector2Int> Join(Section from, Section to, List<Gate> Gates)
        {
            // from 과 to 가 서로 인접한 방향
            var relation = from.AdjoiningWithDirection(to);
            if (relation == Constants.Direction.Error)
            {
                return new Vector2Int[] { };
            }

            var inverse = Constants.Direction.Inverse(relation);

            // From 에서 relation 방향에 연결된 출구가 있거나
            // To 에서 inverse 방향에 연결된 출구가 있다면
            if (!from.ExistUnconnectedJoints(relation) || !to.ExistUnconnectedJoints(inverse))
            {
                return new Vector2Int[] { };
            }

            var start1 = PickJoint(from, relation, Gates);
            var start2 = PickJoint(to, inverse, Gates);
            var end1 = from.GetEdge(to, start1);
            // ex) Left : from.xMin, start1.y
            var end2 = to.GetEdge(from, start2);

            return new[]
            {
                start1.LineTo(end1),
                start2.LineTo(end2),
                end1.LineTo(end2)
            }.SelectMany(p => p);
        }

        private static Vector2Int PickJoint(Section section, int direction, List<Gate> Gates)
        {
            // 현재 section에 Room 이 존재하지 않는다면
            if (!section.ExistRoom)
            {
                // 길의 중계지점이 될 Position을 반환
                return section.Relay.Point;
            }

            var joints = section.ExistUnconnectedJoints(direction) ? section.GetUnConnectedJoints(direction).ToList() : section.GetConnectedJoints(direction).ToList();
            var pick = joints[Random.Range(0, joints.Count)];

            Gates.Add(new Gate { Direction = direction, Position = pick.Position, Room = section.Room });

            pick.Connected = true;
            return pick.Position;
        }

        private static void MakeRoomsType(Field field, FieldBluePrint bluePrint)
        {
            // 방이 존재하는 Section 가져오기
            var sectionIndexes = field.Sections.Where(section => section.ExistRoom).Select(section => section.Index).ToList();
            var result = new Dictionary<RoomType, List<Section>>(); // RoomType, Section
            
            // BluePrint에 따라 RewardType과 ShopType의 방 개수를 랜덤하게 설정
            int rewardRoomCount = Random.Range(bluePrint.MinRewardRoomCount, bluePrint.MaxRewardRoomCount + 1);
            int shopRoomCount   = Random.Range(bluePrint.MinShopRoomCount, bluePrint.MaxShopRoomCount + 1);

            // RoomType 생성
            GenerateRoomType(bluePrint.MinEntrance, RoomType.PlayerSpawnRoom);
            GenerateRoomType(bluePrint.MinExtrance, RoomType.ExitRoom);
            GenerateRoomType(rewardRoomCount, RoomType.RewardRoom);
            GenerateRoomType(shopRoomCount, RoomType.ShopRoom);
            
            // BattleType의 Room 개수 계산 ( -1: 남은 Room을 모두 BattleType으로 )
            int battleRoomCount = (bluePrint.MaxBattleRoomCount == -1)
                ? sectionIndexes.Count
                : Random.Range(bluePrint.MinBattleRoomCount, bluePrint.MaxBattleRoomCount + 1);
            // Battle Room 생성
            GenerateRoomType(battleRoomCount, RoomType.MonsterSpawnRoom);
            GenerateEmptyFromAllLeftRooms(); // 남은 Room을 모두 EmptyType으로
            
            // RoomType에 따라 RoomData를 세팅
            foreach (var pair in result) {
                foreach (var section in pair.Value) {
                    section.Room.SetRoomData(pair.Key, bluePrint.GetRoomEntityByWeight(pair.Key));
                }
            }

            field.RoomDictionary = result;
            return;
            
            void GenerateRoomType(int count, RoomType roomType) {
                while (count > 0) {
                    if (!result.ContainsKey(roomType)) {
                        result.Add(roomType, new List<Section>());
                    }
                    
                    // 남은 Section이 존재하지 않음
                    if (sectionIndexes.Count <= 0) {
                        // 우선순위가 높은 Room이 생성되지 못함
                        if (roomType != RoomType.MonsterSpawnRoom) {
                            CatLog.WLog("Generate RoomType Warning !");
                        }
                        return;
                    }

                    // 무작위 Section Index를 도출하고 Dictionary에 저장
                    int randomSectionIndex = sectionIndexes[Random.Range(0, sectionIndexes.Count)];
                    sectionIndexes.RemoveAll(index => index.Equals(randomSectionIndex));
                    
                    var findSection = field.Sections.Find(section => section.Index == randomSectionIndex);
                    if (findSection == null || findSection.Room.RoomData != null) {
                        CatLog.ELog("Section Find Failed Or Override Room Type Error.");
                        return;
                    }
                    
                    result[roomType].Add(findSection);
                    count--;
                }
            }

            void GenerateEmptyFromAllLeftRooms() {
                result.Add(RoomType.EmptyRoom, new List<Section>());
                for (int i = sectionIndexes.Count - 1; i >= 0; i--) {
                    int sectionIndexNum = sectionIndexes[i];
                    var findSection = field.Sections.Find(section => section.Index == sectionIndexNum);
                    if (findSection == null || findSection.Room.RoomData != null) {
                        CatLog.ELog("Section Find Failed Or Override Room Type Error.");
                        continue;
                    }
                    
                    result[RoomType.EmptyRoom].Add(findSection);
                    sectionIndexes.RemoveAt(i);
                }
            }
        }
    }
}
