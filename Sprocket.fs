FeatureScript 1174;
import(path : "onshape/std/geometry.fs", version : "1174.0");
GearIcon::import(path : "f852cb9213fd45f389ee13c6", version : "d316751df2d27793113d1660");

//based on ASME/ANSI B29.1-2011 Roller Chain Standard Sizes
const Chain_Specs = {
        ChainNumber._15 : { pitch : (3 / 16), roller : 0.098 },
        ChainNumber._25 : { pitch : (1 / 4), roller : 0.130 },
        ChainNumber._35 : { pitch : (3 / 8), roller : 0.200 },
        ChainNumber._40 : { pitch : (1 / 2), roller : 0.306 },
        ChainNumber._41 : { pitch : (1 / 2), roller : 0.3125 },
        ChainNumber._50 : { pitch : (5 / 8), roller : 0.400 },
        ChainNumber._60 : { pitch : (3 / 4), roller : 0.469 },
        ChainNumber._80 : { pitch : 1, roller : 0.625 },
        ChainNumber._100 : { pitch : 1 + (1 / 4), roller : 0.750 },
        ChainNumber._120 : { pitch : 1 + (1 / 2), roller : 0.875 },
        ChainNumber._140 : { pitch : 1 + (3 / 4), roller : 1 },
        ChainNumber._160 : { pitch : 2, roller : 1.125 },
        ChainNumber._180 : { pitch : 2 + (1 / 4), roller : 1.406 },
        ChainNumber._200 : { pitch : 2 + (1 / 2), roller : 1.562 },
        ChainNumber._240 : { pitch : 3, roller : 1.875 }
    };

annotation { "Feature Type Name" : "Sprocket", "Icon" : GearIcon::BLOB_DATA }
export const Spocket = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Choose Center" }
        definition.newCenter is boolean;

        annotation { "Name" : "Plane", "Filter" : EntityType.FACE, "MaxNumberOfPicks" : 1 }
        definition.SelectedPlane is Query;

        if (definition.newCenter == true)
        {
            annotation { "Name" : "Point", "Filter" : EntityType.VERTEX, "MaxNumberOfPicks" : 1 }
            definition.SelectedPoint is Query;
        }

        annotation { "Name" : "Chain Number" }
        definition.ChainNumber is ChainNumber;

        annotation { "Name" : "Teeth" }
        isInteger(definition.Teeth, { (unitless) : [5, 10, 150] } as IntegerBoundSpec);

        annotation { "Name" : "Center Bore" }
        definition.Bore is boolean;

        if (definition.Bore == true)
        {
            annotation { "Name" : "Bore Size" }
            isLength(definition.BoreSize, { (inch) : [0.0001, 0.15, 50] } as LengthBoundSpec);
        }

        annotation { "Name" : "Build 3d" }
        definition.buildButton is boolean;

        if (definition.buildButton == true)
        {

            annotation { "Name" : "End type", "UIHint" : UIHint.HORIZONTAL_ENUM }
            definition.endType is BoundType;

            if (definition.endType == BoundType.BLIND)
            {
                annotation { "Name" : "Opposite Direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.DirectionalExtrude is boolean;
            }

            annotation { "Name" : "Depth" }
            isLength(definition.gearDepth, { (inch) : [0.01, 0.130, 10] } as LengthBoundSpec);

        }
    }
    {
        var plane1 = evPlane(context, {
                "face" : definition.SelectedPlane
            });
        if (definition.newCenter == true)
        {
            if (isPointOnPlane(evVertexPoint(context, { "vertex" : definition.SelectedPoint }), evPlane(context, { "face" : definition.SelectedPlane })) == false)
                throw regenError("Point not on selected plane", ["SelectedPoint"]);
            plane1.origin += evVertexPoint(context, { "vertex" : definition.SelectedPoint }) - plane1.origin;
        }
        var sketch1 = newSketchOnPlane(context, id + "sketch1", { "sketchPlane" : plane1 });

        /*
         **  defintions for variables below and certain lines found here
         **  http://www.gearseds.com/files/design_draw_sprocket_5.pdf
         */
        var P = Chain_Specs[definition.ChainNumber].pitch;
        var N = definition.Teeth;
        var Dr = Chain_Specs[definition.ChainNumber].roller;
        var Ds = 1.0005 * Dr + 0.003;
        var R = Ds / 2;
        var A = (35 + (60 / N)) * degree;
        var B = (18 - (56 / N)) * degree;
        var ac = 0.8 * Dr;
        var M = ac * cos(A);
        var T = ac * sin(A);
        var E = 1.3025 * Dr + 0.0015;
        // var yz = Dr * ((1.4 * (sin((17 - (64 / N)) * degree))) - (0.8 * sin(B)));
        var ab = 1.4 * Dr;
        var W = ab * cos((180 / N) * degree);
        var V = ab * sin((180 / N) * degree);
        var F = Dr * ((0.8 * cos(B)) + (1.4 * cos((17 - (64 / N)) * degree)) - 1.3025) - 0.0015;
        // var H = sqrt(F ^ 2 - (((1.4 * Dr) - (P / 2)) ^ 2));
        // var S = P / 2 * cos((180 / N) * degree) + (H * sin((180 / N) * degree));
        var PD = P / sin((180 / N) * degree);

        //find where line CX intersects circle R
        var pointRCX = (lineCircleIntersect(M, T, M - cos(A), T - sin(A), R, 1));
        pointRCX[1] += PD / 2;
        //define point Y, E distance from point C
        var pointY = [M - (cos(A - B) * E), PD / 2 + T - (sin(A - B)) * E];
        //find center of circles using radius E with point RCX and point Y
        var CENTERS = CircleCenters(pointRCX[0], pointRCX[1], pointY[0], pointY[1], E);
        //find midpoint of arc (RCX - Y) with radius E
        var arcXY_mid = lineCircleIntersect(((pointRCX[0] + pointY[0]) / 2) - CENTERS[2], ((pointRCX[1] + pointY[1]) / 2) - CENTERS[3], 0, 0, E, 1);
        arcXY_mid[0] += CENTERS[2];
        arcXY_mid[1] += CENTERS[3];
        //define point B
        var pointB = [0 - W, PD / 2 - V];
        //find intersect of toothTip on circle B with radius R
        var toothTip = lineCircleIntersect(0 - pointB[0], 0 - pointB[1], (cos((180 / N) * degree) * (0 - 0) - sin((180 / N) * degree) * (PD / 2 + F + .05 - 0) + 0) - pointB[0], (sin((180 / N) * degree) * (0 - 0) + cos((180 / N) * degree) * (PD / 2 + F + .05 - 0) + 0) - pointB[1], F, 2);
        toothTip[0] += pointB[0];
        toothTip[1] += pointB[1];
        //rotate point C about point Y 90 degrees counterclockwise
        var pointZ = [cos(90 * degree) * (M - pointY[0]) - sin(90 * degree) * (PD / 2 + T - pointY[1]) + pointY[0], sin(90 * degree) * (M - pointY[0]) + cos(90 * degree) * (PD / 2 + T - pointY[1]) + pointY[1]];
        var pointBYZ = lineCircleIntersect(0, 0, pointZ[1] - pointY[1], -1 * (pointZ[0] - pointY[0]), F, 2);
        pointBYZ[0] += pointB[0];
        pointBYZ[1] += pointB[1];
        //find midpoint of arc (BYZ - toothTip) with radius F
        var arcToothZ_mid = lineCircleIntersect(((pointBYZ[0] + toothTip[0]) / 2) - pointB[0], ((pointBYZ[1] + toothTip[1]) / 2) - pointB[1], 0, 0, F, 2);
        arcToothZ_mid[0] += pointB[0];
        arcToothZ_mid[1] += pointB[1];
        //find midpoint of arc (R - X) with radius R
        var arcRX_mid = lineCircleIntersect(pointRCX[0] / 2, ((pointRCX[1] + (PD / 2 - R)) / 2) - PD / 2, 0, 0, R, 1);
        arcRX_mid[1] += PD / 2;

        /*
         **  With all these variables now we can
         **  construct our arcs and lines as 3d vectors
         */
        const vRCX = vector(pointRCX[0], pointRCX[1], 0) * inch;
        const vXY_mid = vector(arcXY_mid[0], arcXY_mid[1], 0) * inch;
        const vY = vector(pointY[0], pointY[1], 0) * inch;
        const vBYZ = vector(pointBYZ[0], pointBYZ[1], 0) * inch;
        const vtoothZ_mid = vector(arcToothZ_mid[0], arcToothZ_mid[1], 0) * inch;
        const vTip = vector(toothTip[0], toothTip[1], 0) * inch;
        const vRX_mid = vector(arcRX_mid[0], arcRX_mid[1], 0) * inch;
        const vR = vector(0, PD / 2 - R, 0) * inch;

        //define rotation axis
        const axis = line(vector(0, 0, 0) * inch, vector(0, 0, 1));

        for (var i = 0; i < N; i += 1)
        {
            //label arc and line id's
            const arcXY = "arcXY" ~ i;
            const arcToothZ = "arcToothZ" ~ i;
            const arcRX = "arRX" ~ i;
            const lineYBZ = "lineYBZ" ~ i;
            const arcXY_r = "arcXY_r" ~ i;
            const arcToothZ_r = "arcTooth_rZ" ~ i;
            const arcRX_r = "arRX_r" ~ i;
            const lineYBZ_r = "lineYBZ_r" ~ i;

            //find rotation per itteration
            const rotation = rotationAround(axis, ((i / N) * (360 * degree)));

            //apply rotation to all lines and arcs
            const dRCX = rotation * vRCX;
            const dXY_mid = rotation * vXY_mid;
            const dY = rotation * vY;
            const dBYZ = rotation * vBYZ;
            const dtoothZ_mid = rotation * vtoothZ_mid;
            const dtip = rotation * vTip;
            const drx_mid = rotation * vRX_mid;
            const dr = rotation * vR;

            //draw all lines and mirror of lines per tooth count
            skArc(sketch1, arcXY, {
                        "start" : vector(dRCX[0], dRCX[1]),
                        "mid" : vector(dXY_mid[0], dXY_mid[1]),
                        "end" : vector(dY[0], dY[1])
                    });
            skArc(sketch1, arcToothZ, {
                        "start" : vector(dBYZ[0], dBYZ[1]),
                        "mid" : vector(dtoothZ_mid[0], dtoothZ_mid[1]),
                        "end" : vector(dtip[0], dtip[1])
                    });
            skArc(sketch1, arcRX, {
                        "start" : vector(dRCX[0], dRCX[1]),
                        "mid" : vector(drx_mid[0], drx_mid[1]),
                        "end" : vector(dr[0], dr[1])
                    });
            skLineSegment(sketch1, lineYBZ, {
                        "start" : vector(dBYZ[0], dBYZ[1]),
                        "end" : vector(dY[0], dY[1])
                    });
            skArc(sketch1, arcXY_r, {
                        "start" : vector(-dRCX[0], dRCX[1]),
                        "mid" : vector(-dXY_mid[0], dXY_mid[1]),
                        "end" : vector(-dY[0], dY[1])
                    });
            skArc(sketch1, arcToothZ_r, {
                        "start" : vector(-dBYZ[0], dBYZ[1]),
                        "mid" : vector(-dtoothZ_mid[0], dtoothZ_mid[1]),
                        "end" : vector(-dtip[0], dtip[1])
                    });
            skArc(sketch1, arcRX_r, {
                        "start" : vector(-dRCX[0], dRCX[1]),
                        "mid" : vector(-drx_mid[0], drx_mid[1]),
                        "end" : vector(-dr[0], dr[1])
                    });
            skLineSegment(sketch1, lineYBZ_r, {
                        "start" : vector(-dBYZ[0], dBYZ[1]),
                        "end" : vector(-dY[0], dY[1])
                    });
        }

        skSolve(sketch1);

        if (definition.Bore == true)
        {
            var sketch2 = newSketchOnPlane(context, id + "sketch2", { "sketchPlane" : plane1 });
            if (definition.BoreSize > ((PD / 2 - R) * 0.75) * inch)
                throw regenError("Bore too large", ["BoreSize"]);
            skCircle(sketch2, "circleBore", { "center" : vector(0, 0) * inch, "radius" : definition.BoreSize });
            skSolve(sketch2);
        }
        
        if (definition.buildButton == true)
        {
            var depth = definition.gearDepth;
            if (definition.endType == BoundType.BLIND)
            {
                extrude(context, id + "extrudeGear", { "entities" : qSketchRegion(id + "sketch1"), "endBound" : BoundingType.BLIND, "depth" : depth,
                            "operationType" : NewBodyOperationType.NEW,
                            "oppositeDirection" : definition.DirectionalExtrude });
            }
            else
            {
                extrude(context, id + "extrudeGear", { "entities" : qSketchRegion(id + "sketch1"), "endBound" : BoundingType.SYMMETRIC, "depth" : depth,
                            "operationType" : NewBodyOperationType.NEW });
            }
            if (definition.Bore == true)
            {
                if (definition.endType == BoundType.BLIND)
                {
                    extrude(context, id + "extrudeBore", { "entities" : qSketchRegion(id + "sketch2"), "endBound" : BoundingType.BLIND, "depth" : depth,
                                "defaultScope" : false,
                                "booleanScope" : qCreatedBy(id + "extrudeGear", EntityType.BODY),
                                "operationType" : NewBodyOperationType.REMOVE,
                                "oppositeDirection" : definition.DirectionalExtrude });
                }
                else
                {
                    extrude(context, id + "extrudeBore", { "entities" : qSketchRegion(id + "sketch2"), "endBound" : BoundingType.SYMMETRIC, "depth" : depth,
                                "defaultScope" : false,
                                "booleanScope" : qCreatedBy(id + "extrudeGear", EntityType.BODY),
                                "operationType" : NewBodyOperationType.REMOVE });
                }
            }
        }
    });

/**
 * Return the coordinates of two circles created from points
 * @function CircleCenters
 * @param  {Float} x1   Origin X of line 1.
 * @param  {Float} y1   Origin Y of line 1.
 * @param  {Float} x2   Origin X of line 2.
 * @param  {Float} y2   Origin Y of line 2.
 * @param  {Float} r    Radius of Expected Circle.
 * @return {Array} xa, ya, xb, yb   Centers of both circles.
 */
export function CircleCenters(x1 is number, y1 is number, x2, y2, r)
{
    var q = sqrt((x2 - x1) ^ 2 + (y2 - y1) ^ 2);
    var y3 = (y1 + y2) / 2;
    var x3 = (x1 + x2) / 2;
    var xa = x3 + sqrt(r ^ 2 - (q / 2) ^ 2) * (y1 - y2) / q;
    var ya = y3 + sqrt(r ^ 2 - (q / 2) ^ 2) * (x2 - x1) / q;
    var xb = x3 - sqrt(r ^ 2 - (q / 2) ^ 2) * (y1 - y2) / q;
    var yb = y3 - sqrt(r ^ 2 - (q / 2) ^ 2) * (x2 - x1) / q;
    return [xa, ya, xb, yb];
}

/**
 * Find intersection of slope in a circle
 * (assumes circle at origin 0,0)
 * @function lineCircleIntersect
 * @param  {Float} x1   Origin X of point 1.
 * @param  {Float} y1   Origin Y of point 1.
 * @param  {Float} x2   Origin X of point 2.
 * @param  {Float} y2   Origin Y of point 2.
 * @param  {Float} r    Radius of circle.
 * @param  {Int} secant     Indication of first or second intersection.
 * @return {Array} x, y     Point of intersection.
 */
function lineCircleIntersect(x1, y1, x2, y2, r, secant)
{
    var dx = x2 - x1;
    var dy = y2 - y1;
    var dr = sqrt(dx ^ 2 + dy ^ 2);
    var D = x1 * y2 - x2 * y1;
    var x;
    var y;
    if (secant == 1)
    {
        x = (D * dy - (dy < 0 ? -1 : 1) * dx * sqrt(r ^ 2 * dr ^ 2 - D ^ 2)) / dr ^ 2;
        y = (-D * dx - (dy < 0 ? -dy : dy) * sqrt(r ^ 2 * dr ^ 2 - D ^ 2)) / dr ^ 2;
    }
    else
    {
        x = (D * dy + (dy < 0 ? -1 : 1) * dx * sqrt(r ^ 2 * dr ^ 2 - D ^ 2)) / dr ^ 2;
        y = (-D * dx + (dy < 0 ? -dy : dy) * sqrt(r ^ 2 * dr ^ 2 - D ^ 2)) / dr ^ 2;
    }
    return ([x, y]);
}

export enum BoundType
{
    annotation { "Name" : "Blind" }
    BLIND,
    annotation { "Name" : "Symmetric" }
    SYMMETRIC
}

export enum ChainNumber
{
    annotation { "Name" : "15" }
    _15,
    annotation { "Name" : "25" }
    _25,
    annotation { "Name" : "35" }
    _35,
    annotation { "Name" : "40" }
    _40,
    annotation { "Name" : "41" }
    _41,
    annotation { "Name" : "50" }
    _50,
    annotation { "Name" : "60" }
    _60,
    annotation { "Name" : "80" }
    _80,
    annotation { "Name" : "100" }
    _100,
    annotation { "Name" : "120" }
    _120,
    annotation { "Name" : "140" }
    _140,
    annotation { "Name" : "160" }
    _160,
    annotation { "Name" : "180" }
    _180,
    annotation { "Name" : "200" }
    _200,
    annotation { "Name" : "240" }
    _240
}
