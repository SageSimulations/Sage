/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Highpoint.Sage.Mathematics {
    /// <summary>
    /// Summary description for zTestInterpolations.
    /// </summary>
    [TestClass]
    public class Interpolations101 {
        public Interpolations101() {Init();}

		[TestInitialize] 
		public void Init() {
		}
		[TestCleanup]
		public void destroy() {
			Debug.WriteLine( "Done." );
		}
		
		/// <summary>
		/// One line segment defined, tests outsides, vertices and middle of the segment.
		/// </summary>
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("One line segment defined, tests outsides, vertices and middle of the segment")]
		public void TestInterpolationFrom2Points(){
			IWriteableInterpolable interp = new SmallDoubleInterpolable(2);
			IInterpolable interp2 = (IInterpolable)interp;
			interp.SetYValue(3.3,6.6);
			interp.SetYValue(4.4,8.8);

			Verify(interp2,5.5,11);
			Verify(interp2,4.4,8.8);
			Verify(interp2,3.85,7.7);
			Verify(interp2,3.3,6.6);
			Verify(interp2,2.2,4.4);
			Verify(interp2,0.0,0.0);
			Verify(interp2,-2.2,-4.4);
            
		}

		/// <summary>
		/// One line segment defined, tests outsides, vertices and middle of the segment.
		/// </summary>
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("One line segment defined, Replace each Y value, ensure that it was actually replaced.")]
		public void TestInterpolationPointReplacement(){
			IWriteableInterpolable interp = new SmallDoubleInterpolable(2);
			IInterpolable interp2 = (IInterpolable)interp;
			interp.SetYValue(3.3,6.6);
			interp.SetYValue(4.4,8.8);
			interp.SetYValue(7.7,12.8);
			interp.SetYValue(13.2,22.8);    
        
			interp.SetYValue(7.7,17.8);
			Verify(interp2,8.8,18.8);

		}

		/// <summary>
		/// One line segment defined, tests outsides, vertices and middle of the segment with negative slope.
		/// </summary>
		[TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("One line segment defined, tests outsides, vertices and middle of the segment with negative slope")]
		public void TestInterpolationFrom2PointsNegativeSlope(){
			IWriteableInterpolable interp = new SmallDoubleInterpolable(2);
			IInterpolable interp2 = (IInterpolable)interp;
			interp.SetYValue(3.3,8.8);
			interp.SetYValue(4.4,6.6);


			Verify(interp2,5.5,4.4);
			Verify(interp2,4.4,6.6);
			Verify(interp2,3.85,7.7);
			Verify(interp2,3.3,8.8);
			Verify(interp2,2.2,11);
			Verify(interp2,0.0,15.4);
			Verify(interp2,-2.2,19.8);
			Verify(interp2,10.0,-4.6);
            
		}

		/// <summary>
        /// Three line segments defined, tests outsides, vertices and middles of each.
        /// </summary>
        [TestMethod]
		[Highpoint.Sage.Utility.FieldDescription("Three line segments defined, tests outsides, vertices and middles of each")]
        public void TestInterpolationFrom4Points(){
            IWriteableInterpolable interp = new SmallDoubleInterpolable(4);
            IInterpolable interp2 = (IInterpolable)interp;
            interp.SetYValue(4.8,2.7);
            interp.SetYValue(3.2,1.5);
            interp.SetYValue(6.0,4.5);
            interp.SetYValue(0.8,0.6);

			Verify(interp2,7.2,6.3);
			Verify(interp2,6.0,4.5);
			Verify(interp2,5.2,3.3);
			Verify(interp2,4.8,2.7);
			Verify(interp2,4.0,2.1);
			Verify(interp2,3.2,1.5);
			Verify(interp2,1.6,0.9);
			Verify(interp2,0.8,0.6);
			Verify(interp2,0.0,0.3);
			Verify(interp2,-0.8,0.0);
            
        }

		private void Verify(IInterpolable interp, double xValue, double expectedYValue){
            Assert.IsTrue(Math.Abs(interp.GetYValue(xValue) - expectedYValue  ) < 0.000001 ,"Point x = "+xValue+" does not result in y = "+expectedYValue+".");
		}
    }
}
