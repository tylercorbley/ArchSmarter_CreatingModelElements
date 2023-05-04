#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace ArchSmarter_CreatingModelElements
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Your code goes here
            UIDocument uidoc = uiapp.ActiveUIDocument;
            IList<Element> pickList = uidoc.Selection.PickElementsByRectangle("Select elements");

            TaskDialog.Show("Test", "I selected " + pickList.Count.ToString() + " elements");

            //2 filter through selected elements strategy one, gets all lines
            List<CurveElement> allCurves = new List<CurveElement>();
            foreach (Element elem in pickList)
            {
                if (elem is CurveElement)
                {
                    allCurves.Add(elem as CurveElement);
                }
            }

            //2 strategy two - gets specific model lines
            List<CurveElement> modelCurves = new List<CurveElement>();
            foreach (Element elem in allCurves)
            {
                if (elem is CurveElement)
                {
                    CurveElement curveElem = elem as CurveElement;
                    if (curveElem.CurveElementType == CurveElementType.ModelCurve)
                    {
                        modelCurves.Add(curveElem);
                    }
                }
            }

            //3 curve data
            foreach (CurveElement currentCurve in modelCurves)
            {
                Curve curve = currentCurve.GeometryCurve;
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                GraphicsStyle curStyle = currentCurve.LineStyle as GraphicsStyle;

                Debug.Print(curStyle.Name);
            }


            //5 transaction
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create Revit Elements");
                //4 create wall
                Level newLevel = Level.Create(doc, 20);
                Curve curCurve1 = modelCurves[0].GeometryCurve;
                Wall.Create(doc, curCurve1, newLevel.Id, false);

                FilteredElementCollector wallTypes = new FilteredElementCollector(doc);
                Curve curCurve2 = modelCurves[1].GeometryCurve;
                WallType myWallType = GetWallTypeByName(doc, "Basic Wall");
                Wall.Create(doc, curCurve2, myWallType.Id, newLevel.Id, 20, 0, false, false);

                //6 get system type
                FilteredElementCollector systemCollector = new FilteredElementCollector(doc);
                systemCollector.OfClass(typeof(MEPSystemType));

                //7 get duct system type
                MEPSystemType ductSystemType = null;
                foreach (MEPSystemType curType in systemCollector)
                {
                    if (curType.Name == "Supply Air")
                    {
                        ductSystemType = curType;
                        break;
                    }
                }



                //8 get duct type
                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                collector1.OfClass(typeof(DuctType));

                //9 create duct
                Curve curCurve3 = modelCurves[2].GeometryCurve;
                Duct newDuct = Duct.Create(doc, ductSystemType.Id, collector1.FirstElementId(), newLevel.Id, curCurve3.GetEndPoint(0), curCurve3.GetEndPoint(1));

                //10 get pipe system type
                MEPSystemType pipeSystemType = null;
                foreach (MEPSystemType curType in systemCollector)
                {
                    if (curType.Name == "Domestic Hot Water")
                    {
                        pipeSystemType = curType;
                        break;
                    }
                }

                //11 get pipe type
                FilteredElementCollector collector2 = new FilteredElementCollector(doc);
                collector2.OfClass(typeof(PipeType));

                //12 create pipe
                Curve curCurve4 = modelCurves[3].GeometryCurve;
                Pipe.Create(doc, pipeSystemType.Id, collector2.FirstElementId(), newLevel.Id, curCurve4.GetEndPoint(0), curCurve4.GetEndPoint(1));

                //15 switch
                int numberValue = 5;
                string numAsString = "";

                switch (numberValue)
                {
                    case 1:
                        numAsString = "One";
                        break;
                    case 2:
                        numAsString = "Two";
                        break;
                    case 3:
                        numAsString = "Three";
                        break;
                    case 4:
                        numAsString = "Four";
                        break;
                    case 5:
                        numAsString = "Five";
                        break;
                    default:
                        numAsString = "Zero";
                        break;
                }

                //16 advanced switch
                Curve curve5 = modelCurves[1].GeometryCurve;
                GraphicsStyle curve5GS = modelCurves[1].LineStyle as GraphicsStyle;

                WallType wallType1 = GetWallTypeByName(doc, "Storefront");
                WallType wallType2 = GetWallTypeByName(doc, "Exterior - Brick on CMU");

                switch (curve5GS.Name)
                {
                    case "<Thin Lines>":
                        Wall.Create(doc, curve5, wallType1.Id, newLevel.Id, 20, 0, false, false);
                        break;
                    case "<Wide LInes>":
                        Wall.Create(doc, curve5, wallType2.Id, newLevel.Id, 20, 0, false, false);
                        break;
                    default:
                        Wall.Create(doc, curve5, newLevel.Id, false);
                        break;
                }

                t.Commit();
            }

            return Result.Succeeded;
        }

        internal WallType GetWallTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(WallType));

            foreach (WallType curType in collector)
            {
                if (curType.Name == typeName)
                {
                    return curType;
                }
            }
            return null;
        }


        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
