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
    public class Command2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Your code goes here

            //1 create pick list
            UIDocument uidoc = uiapp.ActiveUIDocument;
            IList<Element> pickList = uidoc.Selection.PickElementsByRectangle("Select elements");

            TaskDialog.Show("Test", "I selected " + pickList.Count.ToString() + " elements");

            //2 strategy two - gets specific model lines
            List<CurveElement> modelCurves = new List<CurveElement>();
            foreach (Element elem in pickList)
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
                //XYZ startPoint = curve.GetEndPoint(0);
                //XYZ endPoint = curve.GetEndPoint(1);

                GraphicsStyle curStyle = currentCurve.LineStyle as GraphicsStyle;

                Debug.Print(curStyle.Name);
            }

            //5 transaction
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create Revit Elements");
                Level newLevel = Level.Create(doc, 0);

                //8 get duct type
                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                collector1.OfClass(typeof(DuctType));
                //________________________________________________________________________
                //11 get pipe type
                FilteredElementCollector collector2 = new FilteredElementCollector(doc);
                collector2.OfClass(typeof(PipeType));
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //16 advanced switch
                WallType wallType1 = GetWallTypeByName(doc, "Storefront");
                WallType wallType2 = GetWallTypeByName(doc, "Generic - 8\"");
                MEPSystemType pipeType1 = GetMEPSystemTypeByName(doc, "Default");
                MEPSystemType ductType1 = GetMEPSystemTypeByName(doc, "Default");

                //
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

                MEPSystemType pipeSystemType = null;
                foreach (MEPSystemType curType in systemCollector)
                {
                    if (curType.Name == "Domestic Hot Water")
                    {
                        pipeSystemType = curType;
                        break;
                    }
                }
                //

                foreach (CurveElement currentCurve in modelCurves)
                {
                    Curve curve = currentCurve.GeometryCurve;
                    GraphicsStyle curStyle = currentCurve.LineStyle as GraphicsStyle;
                    switch (curStyle.Name)
                    {
                        case "A-GLAZ":
                            Wall.Create(doc, curve, wallType1.Id, newLevel.Id, 20, 0, false, false);
                            break;
                        case "A-WALL":
                            Wall.Create(doc, curve, wallType2.Id, newLevel.Id, 20, 0, false, false);
                            break;
                        case "M-DUCT":
                            Duct.Create(doc, ductSystemType.Id, collector1.FirstElementId(), newLevel.Id, curve.GetEndPoint(0), curve.GetEndPoint(1));
                            break;
                        case "P-PIPE":
                            Pipe.Create(doc, pipeSystemType.Id, collector2.FirstElementId(), newLevel.Id, curve.GetEndPoint(0), curve.GetEndPoint(1));
                            break;
                    }
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
        //___________________________________________________________________________________
        //get wall type method
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
        //filtered element collector method
        //internal FilteredElementCollector FEC(Document doc, string systemName)
        //{
        //    FilteredElementCollector collector = new FilteredElementCollector(doc);
        //    if systemName = PipeType then pipetype else DuctType or something like that
        //    return collector.OfClass(typeof(systemName));
        //}
        // pipe method
        internal MEPSystemType GetMEPSystemTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector systemCollector = new FilteredElementCollector(doc);
            systemCollector.OfClass(typeof(MEPSystemType));

            foreach (MEPSystemType curType in systemCollector)
            {
                if (curType.Name == typeName)
                {
                    return curType;
                }
            }
            return null;
        }
        // duct method
        //internal DuctType GetDuctSystemTypeByName(Document doc, string typeName)
        //{
        //    FilteredElementCollector systemCollector = new FilteredElementCollector(doc);
        //    systemCollector.OfClass(typeof(MEPSystem));

        //    foreach (DuctType curType in systemCollector)
        //    {
        //        if (curType.Name == typeName)
        //        {
        //            return curType;
        //        }
        //    }
        //    return null;
        //}

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
