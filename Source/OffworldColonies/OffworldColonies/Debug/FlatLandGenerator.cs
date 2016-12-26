using System.Collections.Generic;
using ModUtils;
using UnityEngine;

namespace OffworldColoniesPlugin.Debug {
    /// <summary>
    /// Functor to recursively add land tiles around the initial position using the given angle increments
    /// </summary>
    public class FlatLandGenerator {
        public static List<Vector3> GenerateBasePositions(
            Vector3 initialPosition, 
            int recursionLevels, 
            Vector3 newPosOffset, 
            int angleStart = -60, 
            int angleEnd = 60, 
            int angleIncrement = 60
        ) {

            float timeTaken = Time.realtimeSinceStartup;

            List<Vector3> positions = GeneratePositionsRecursively(initialPosition, recursionLevels, newPosOffset, angleStart, angleEnd, angleIncrement);

            positions.Add(initialPosition);

            List<Vector3> finalPosList = new List<Vector3>(positions);
            foreach (Vector3 vec in positions) {
                if (finalPosList.FindAll(p => Vector3.Distance(p, vec) < 1f).Count > 1) {
                    finalPosList.Remove(vec);
                }
            }

            finalPosList.Sort((p, q) => Vector3.Distance(initialPosition, p).CompareTo(Vector3.Distance(initialPosition, q)));

            ModLogger.Log("Creation time:" + (Time.realtimeSinceStartup - timeTaken));

            return finalPosList;
        }


        private static List<Vector3> GeneratePositionsRecursively(Vector3 initialPosition, int recursionLevels, Vector3 newPosOffset, int angleStart, int angleEnd, int angleIncrement) {
            List<Vector3> positions = new List<Vector3>();
            if (recursionLevels-- > 0) {
                List<Vector3> currentChildrenPositions = new List<Vector3>();
                for (int yAngle = angleStart; yAngle <= angleEnd; yAngle += angleIncrement) {

                    Vector3 position = initialPosition + (Quaternion.Euler(0, yAngle, 0) * newPosOffset);

                    positions.Add(position);
                    currentChildrenPositions.Add(position);
                }
                foreach (Vector3 pos in currentChildrenPositions)
                    positions.AddRange(GeneratePositionsRecursively(pos, recursionLevels, newPosOffset, angleStart,angleEnd,angleIncrement));
            }
            return positions;
        }
    }
}
