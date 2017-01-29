using System.Collections.Generic;
using UnityEngine;

namespace OffworldColonies.ColonyManagement {
    /// <summary>
    ///     Functor to recursively add land tiles around the initial position using the given angle increments
    /// </summary>
    public class HexTileGrid {
        private static ColonyManager CM => ColonyManager.Instance;
        private static readonly float Sqrt3Div2 = Mathf.Sqrt(3)*.5f;

        public List<Vector3> Positions { get; }

        public int RingCount { get; private set; }

        /// <summary>
        /// Generates a list of local positions for hex tile placement.
        /// </summary>
        /// <param name="numRings">The number of rings to generate</param>
        /// <returns>A list of positions that form a hex grid with the number of rings specified</returns>
        public HexTileGrid(int numRings) { 
            Positions = new List<Vector3> { Vector3.zero};
            RingCount = numRings;

            for (int ringNum = 1; ringNum < numRings; ringNum++)
                Positions.AddRange(GenerateRing(ringNum));
        }


        private List<Vector3> GenerateRing(int ringRadius) {
            //Add the inital tile and generate the central row
            List<Vector3> positions = new List<Vector3> {
                Vector3.right*(ringRadius*CM.BaseRadius*2),
                Vector3.right*(ringRadius*-CM.BaseRadius*2),
            };

            //Generate remaining rows
            //Length of the current column being generated (amount of tiles)
            int columnLength = ringRadius * 2 - 1;

            //This loops runs once for each column and generates
            //the tile coordinates for this column and row. 
            //Also generate the symmetric tile oppposite
            for (int colIndex = 0; colIndex < ringRadius; colIndex++) {
                for (int rowIndex = 0; rowIndex <= columnLength; rowIndex++) {
                    if (colIndex >= 0 &&
                        colIndex < ringRadius - 1 &&
                        rowIndex > 0 &&
                        rowIndex < columnLength)
                        continue;
                    //If we are past halfway reverse the vertical displacement direction
                    int vDirection = (colIndex < ringRadius ? 1 : -1);
                    float vDisplacement = Sqrt3Div2 * vDirection * (colIndex + 1);
                    float hDisplacement = ringRadius - rowIndex - 0.5f * (colIndex + 1);

                    Vector3 offset = new Vector3(hDisplacement, 0, vDisplacement);

                    positions.Add(offset * CM.BaseRadius*2);
                    positions.Add(offset * -CM.BaseRadius*2);
                }
                //reduce the column length after each column
                columnLength--;
            }
            return positions;
        }

        public void AddRing() {
            List<Vector3> ring = GenerateRing(++RingCount);
            Positions.AddRange(ring);
        }
    }
}