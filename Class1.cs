using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

[Transaction(TransactionMode.Manual)]
public class PlaceSprinklers : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
        Document doc = uiDoc.Document;

        // Выбираем область, в которой будут расставлены спринклеры
        TaskDialog.Show("ОК", "Выберите область в которой будут расставлены спринклеры");
        Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите область для расстановки спринклеров");//выбираем один элемент
        Element elem = doc.GetElement(pickedRef); //преобразуем выбранный элемент в тип Element
        BoundingBoxXYZ bb = elem.get_BoundingBox(null); //получаем boundingbox этого элемента

        // Находим все потолочные элементы в проекте (получаем список Id элементов потолка вр всем проекте)

        List<ElementId> ceilingIds = new FilteredElementCollector(doc)
            .OfClass(typeof(CeilingAndFloor))
            .ToElementIds()
            .ToList();
        

        // Создаем список точек, где будут расставлены спринклеры
        List<XYZ> sprinklerLocations = new List<XYZ>();
        double spacing = 10; // расстояние между спринклерами

        for (double x = bb.Min.X + spacing / 2; x < bb.Max.X; x += spacing)
        {
            for (double y = bb.Min.Y + spacing / 2; y < bb.Max.Y; y += spacing)
            {
                sprinklerLocations.Add(new XYZ(x, y, bb.Max.Z));
            }
        }

        TaskDialog.Show("2", "Приступаем к расстановке спринклеров");

        // Создаем спринклеры на выбранных местах
        using (Transaction trans = new Transaction(doc, "Place Sprinklers"))
        {
            trans.Start();
            foreach (XYZ loc in sprinklerLocations)
            {
                // Находим потолочный элемент под каждой точкой и создаем спринклер на этом элементе
                CeilingAndFloor ceiling = null;
                foreach (ElementId ceilingId in ceilingIds)
                {
                    CeilingAndFloor testCeiling = doc.GetElement(ceilingId) as CeilingAndFloor;
                    if (testCeiling != null /*&& IsInside(testCeiling.get_BoundingBox(null),loc)*/)
                    {
                        ceiling = testCeiling;
                        //TaskDialog.Show("3", "Потолок не найден");
                        break;
                    }
                }

                if (ceiling != null)
                {
                    FamilySymbol sprinklerSymbol = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_Sprinklers)
                        .FirstElement() as FamilySymbol;
                   // TaskDialog.Show("4", "Потолок найден");

                    if (sprinklerSymbol != null && sprinklerSymbol.IsActive)
                    {
                        doc.Create.NewFamilyInstance(loc, sprinklerSymbol, ceiling, StructuralType.NonStructural);
                       // TaskDialog.Show("5", "Оросители расставлены");
                    }
                }
            }
          //  TaskDialog.Show("6", "Оросители расставлены");
            trans.Commit();
        }

        return Result.Succeeded;
    }

    public static bool IsInside(BoundingBoxXYZ boundingBox, XYZ point)
    {
        if (boundingBox == null || point == null)
        {
            return false;
        }

        return (point.X > boundingBox.Min.X && point.X < boundingBox.Max.X
            && point.Y > boundingBox.Min.Y && point.Y < boundingBox.Max.Y
            && point.Z > boundingBox.Min.Z && point.Z < boundingBox.Max.Z);
    }

    //public static bool IsInside(Element elem, XYZ point)
    //{
    //    Options opt = new Options();
    //    opt.ComputeReferences = true;
    //    opt.IncludeNonVisibleObjects = true;

    //    GeometryElement geoElem = elem.get_Geometry(opt);
    //    foreach (GeometryObject geoObj in geoElem)
    //    {
    //        Solid solid = geoObj as Solid;
    //        if (solid != null)
    //        {
    //            if (solid.ContainsPoint(point))
    //            {
    //                return true;
    //            }
    //        }
    //    }

    //    return false;
    //}

    //public bool ContainsPoint(XYZ point)
    //{
    //    // Получаем тело объекта Solid
    //    GeometryElement geometryElement = this.Geometry;
    //    Solid solid = null;

    //    foreach (GeometryObject geometryObject in geometryElement)
    //    {
    //        solid = geometryObject as Solid;

    //        if (solid != null && solid.Volume > 0)
    //        {
    //            // Если найдено тело с ненулевым объемом, прерываем поиск
    //            break;
    //        }
    //        else
    //        {
    //            solid = null;
    //        }
    //    }

    //    if (solid == null)
    //    {
    //        // Если не найдено тело с ненулевым объемом, возвращаем false
    //        return false;
    //    }

    //    // Создаем полубесконечный луч, направленный от точки вдоль оси Z
    //    XYZ rayDirection = new XYZ(0, 0, 1);
    //    Line ray = Line.CreateBound(point, point + 1000 * rayDirection);

    //    // Используем метод IntersectWith для проверки пересечения тела Solid с лучом
    //    // Если количество пересечений нечетное, то точка находится внутри тела, иначе - снаружи
    //    int intersectionCount = 0;

    //    foreach (Face face in solid.Faces)
    //    {
    //        IntersectionResultArray results;
    //        if (face.Intersect(ray, out results) == SetComparisonResult.Subset)
    //        {
    //            intersectionCount += results.Size;
    //        }
    //    }

    //    return intersectionCount % 2 != 0;
    //}

}
