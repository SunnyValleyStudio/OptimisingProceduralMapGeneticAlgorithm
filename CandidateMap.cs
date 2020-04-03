/*
	Made by Sunny Valle Studio
	(https://svstudio.itch.io)
*/
using SVS.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SVS.ChessMaze
{
	public class CandidateMap
	{
		private MapGrid grid;
		private int numberOfPieces = 0;
		private bool[] obstaclesArray = null;
		private Vector3 startPoint, exitPoint;
		private List<KnightPiece> knightPiecesList;
		private List<Vector3> path = new List<Vector3>();

		private List<Vector3> cornersList;
		private int cornersNearEachOtherCount;


		public MapGrid Grid { get => grid; }
		public bool[] ObstaclesArray { get => obstaclesArray;}

		public CandidateMap(MapGrid grid, int numberOfPieces)
		{
			this.numberOfPieces = numberOfPieces;
			
			this.grid = grid;
		}

		public void CreateMap(Vector3 startPosition, Vector3 exitPosition, bool autoRepair = false)
		{
			this.startPoint = startPosition;
			this.exitPoint = exitPosition;
			obstaclesArray = new bool[grid.Width * grid.Length];
			this.knightPiecesList = new List<KnightPiece>();
			RandomlyPlaceKnightPieces(this.numberOfPieces);

			PlaceObstacles();
			FindPath();

			if (autoRepair)
			{
				Repair();
			}
		}

		public void FindPath()
		{
			this.path = Astar.GetPath(startPoint, exitPoint, obstaclesArray, grid);
			this.cornersList = GetListOfCorners(this.path);
			this.cornersNearEachOtherCount = CalculateCornersNearEachOther(this.cornersList);

		}

		private int CalculateCornersNearEachOther(List<Vector3> cornersList)
		{
			int conrersNearEachOther = 0;
			for (int i = 0; i < cornersList.Count-1; i++)
			{
				if (Vector3.Distance(cornersList[i], cornersList[i + 1]) <= 1)
				{
					conrersNearEachOther++;
				}
			}
			return conrersNearEachOther;
		}

		private List<Vector3> GetListOfCorners(List<Vector3> path)
		{
			List<Vector3> pathWithStart = new List<Vector3>(path);
			pathWithStart.Insert(0, startPoint);
			List<Vector3> cornersPositions = new List<Vector3>();
			if (pathWithStart.Count <= 0)
			{
				return cornersPositions;
			}
			for (int i = 0; i < pathWithStart.Count-2; i++)
			{
				if(pathWithStart[i+1].x > pathWithStart[i].x || pathWithStart[i+1].x < pathWithStart[i].x)
				{
					if(pathWithStart[i+2].z>pathWithStart[i+1].z || pathWithStart[i+2].z < pathWithStart[i + 1].z)
					{
						cornersPositions.Add(pathWithStart[i + 1]);
					}
				}
				else if(pathWithStart[i + 1].z > pathWithStart[i].z || pathWithStart[i + 1].z < pathWithStart[i].z)
				{
					if (pathWithStart[i + 2].x > pathWithStart[i + 1].x || pathWithStart[i + 2].x < pathWithStart[i + 1].x)
					{
						cornersPositions.Add(pathWithStart[i + 1]);
					}
				}
			}

			return cornersPositions;
		}

		private bool CheckIfPositionCanBeObstacle(Vector3 position)
		{
			if(position == startPoint || position == exitPoint)
			{
				return false;
			}
			int index = grid.CalculateIndexFromCoordinates(position.x, position.z);

			return obstaclesArray[index] == false;
		}

		private void RandomlyPlaceKnightPieces(int numbeOfPieces)
		{
			var count = numberOfPieces;
			var knighPlacementTryLimit = 100;
			while (count > 0 && knighPlacementTryLimit>0)
			{
				var randomIndex = Random.Range(0, obstaclesArray.Length);
				if (obstaclesArray[randomIndex] == false)
				{
					var coordinates = grid.CalculateCoordinatesFromIndex(randomIndex);
					if(coordinates==startPoint || coordinates == exitPoint)
					{
						continue;
					}
					obstaclesArray[randomIndex] = true;
					knightPiecesList.Add(new KnightPiece(coordinates));
					count--;

				}
				knighPlacementTryLimit--;
			}
		}

		private void PlaceObstaclesForThisKnight(KnightPiece knight)
		{
			foreach (var position in KnightPiece.listOfPossibleMoves)
			{
				var newPosition = knight.Position + position;
				if(grid.IsCellValid(newPosition.x,newPosition.z) && CheckIfPositionCanBeObstacle(newPosition))
				{
					obstaclesArray[grid.CalculateIndexFromCoordinates(newPosition.x, newPosition.z)] = true;
				}
			}
		}

		private void PlaceObstacles()
		{
			foreach (var knight in knightPiecesList)
			{
				PlaceObstaclesForThisKnight(knight);
			}
		}

		public MapData ReturnMapData()
		{
			return new MapData
			{
				obstacleArray = this.obstaclesArray,
				knightPiecesList = knightPiecesList,
				startPosition = startPoint,
				exitPosition = exitPoint,
				path = this.path,
				cornersList = this.cornersList,
				cornersNearEachOther = this.cornersNearEachOtherCount
			};
		}

		public List<Vector3> Repair()
		{
			int numberOfObstacles = obstaclesArray.Where(obstacle => obstacle).Count();
			List<Vector3> listOfObstaclesToRemove = new List<Vector3>();
			if(path.Count <= 0)
			{
				do
				{
					int obstacleIndexToRemove = Random.Range(0, numberOfObstacles);
					for (int i = 0; i < obstaclesArray.Length; i++)
					{
						if (obstaclesArray[i])
						{
							if (obstacleIndexToRemove == 0)
							{
								obstaclesArray[i] = false;
								listOfObstaclesToRemove.Add(grid.CalculateCoordinatesFromIndex(i));
								break;
							}
							obstacleIndexToRemove--;
						}
					}

					FindPath();
				} while (this.path.Count <= 0);
			}
			foreach (var obstaclePosition in listOfObstaclesToRemove)
			{
				if (path.Contains(obstaclePosition) == false)
				{
					int index = grid.CalculateIndexFromCoordinates(obstaclePosition.x, obstaclePosition.z);
					obstaclesArray[index] = true;
				}
			}

			return listOfObstaclesToRemove;
		}

		public bool IsObstacleAt(int i)
		{
			return obstaclesArray[i];
		}

		public void PlaceObstacle(int i, bool isObstacle)
		{
				obstaclesArray[i] = isObstacle;
		}

		public void AddMutation(double mutationRate)
		{
			int numItems = (int)(obstaclesArray.Length*mutationRate);
			while (numItems > 0)
			{
				int randomIndex = Random.Range(0, obstaclesArray.Length);
				obstaclesArray[randomIndex] = !obstaclesArray[randomIndex];
				numItems--;
			}
		}

		public CandidateMap DeepClone()
		{
			return new CandidateMap(this);
		}

		public CandidateMap(CandidateMap candidateMap)
		{
			this.grid = candidateMap.grid;
			this.startPoint = candidateMap.startPoint;
			this.exitPoint = candidateMap.exitPoint;
			this.obstaclesArray = (bool[])candidateMap.obstaclesArray.Clone();
			this.cornersList = new List<Vector3>(candidateMap.cornersList);
			this.cornersNearEachOtherCount = candidateMap.cornersNearEachOtherCount;
			this.path = new List<Vector3>(candidateMap.path);
		}
	}


}

