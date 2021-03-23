using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.Linq;

[System.Serializable]
public class ExtraerHoja : System.IDisposable
{
    public ConfigExtraerHoja config;
    public ExtraerHoja(ConfigExtraerHoja config, bool autoIniciar=true) {
        this.config = config;
        if(autoIniciar)Iniciar();
    }

    // [SerializeField] float _sizeLimit = 480;
    public float SizeLimit
    {
        get => config._sizeLimit;
        //set => _sizeLimit = value;
    }
    // [SerializeField] float _cannyBajo = 100;
    public float CannyBajo
    {
        get => config._cannyBajo;
        // set => _cannyBajo = value;
    }
    // [SerializeField] float _cannyAlto = 200;
    public float CannyAlto
    {
        get => config._cannyAlto;
        // set => _cannyAlto = value;
    }
    // [SerializeField] int _apertureCanny = 3;
    public int ApertureCanny
    {
        get => config._apertureCanny;
        // set => _apertureCanny = value;
    }
    // [SerializeField] bool _cannyL2 = false;
    public bool CannyL2
    {
        get => config._cannyL2;
        // set => _cannyL2 = value;
    }
    // [SerializeField] bool _cannyDilate = true;
    public bool CannyDilate
    {
        get => config._cannyDilate;
        // set => _cannyDilate = value;
    }

    Mat matImagenLowRes,
        matCannyAndOthers,
        matMask,
        matSalida,
        matClean;
    Point2f puntoDeInteresEscalado = new Point2f();
    Point2f[] quadDetectado = new Point2f[4], quadOrdenado = new Point2f[4], rectDestino = new Point2f[4];
    Point2f tl,tr,bl,br;
    double hullLength,approxPolyStep,maxWidth,maxHeight;
    Scalar white = new Scalar(255), black = new Scalar(0);

    public void Dispose()
    {
        matImagenLowRes?.Dispose();
        matCannyAndOthers?.Dispose();
        matMask?.Dispose();
        matSalida?.Dispose();
        matClean?.Dispose();
    }

    void Iniciar()
    {
        if (matImagenLowRes == null) matImagenLowRes = new Mat();
        if (matCannyAndOthers == null) matCannyAndOthers = new Mat();
        if (matMask == null) matMask = new Mat(1,1,MatType.CV_8UC1);
        if (matSalida == null) matSalida = new Mat();
        if (matClean == null) matClean = new Mat();
    }

    public Mat Procesar(Mat matImagenFuente, Point2f puntoDeInteres, Texture2D[] texturesObserve = null, bool soloMarcar = false)
    {
        Iniciar();

        var escalaReduccionImagen = 1f;
        if (matImagenFuente.Width > SizeLimit || matImagenFuente.Height > SizeLimit)
        {
            escalaReduccionImagen = Mathf.Min(SizeLimit / matImagenFuente.Width, SizeLimit / matImagenFuente.Height);
            Cv2.Resize(matImagenFuente, matImagenLowRes, new Size(matImagenFuente.Width * escalaReduccionImagen, matImagenFuente.Height * escalaReduccionImagen));
        }
        else Cv2.Resize(matImagenFuente, matImagenLowRes, matImagenFuente.Size());

        (puntoDeInteresEscalado.X, puntoDeInteresEscalado.Y) = (puntoDeInteres.X * matImagenLowRes.Width, puntoDeInteres.Y * matImagenLowRes.Height);

        Cv2.Canny(matImagenLowRes, matCannyAndOthers, CannyBajo, CannyAlto, ApertureCanny, CannyL2);
        matCannyAndOthers.Circle(puntoDeInteresEscalado, 9, black, -1);
        if (CannyDilate) Cv2.Dilate(matCannyAndOthers, matCannyAndOthers, matClean);

        matMask.Dispose();
        matMask = new Mat(matCannyAndOthers.Rows + 2,matCannyAndOthers.Cols + 2,matCannyAndOthers.Type());
        // Cv2.Resize(matMask, matMask, new Size(matCannyAndOthers.Cols + 2, matCannyAndOthers.Rows + 2));
        matMask.SetTo(black);

        if (texturesObserve!=null) texturesObserve[0] = OpenCvSharp.Unity.MatToTexture(matCannyAndOthers,texturesObserve[0]);

        Cv2.FloodFill(matCannyAndOthers, matMask, puntoDeInteresEscalado, white);
        matMask.Rectangle(new OpenCvSharp.Rect(0, 0, matMask.Width, matMask.Height), black);
        matMask *= 255;

        Mat[] matContornos;
        Cv2.FindContours(matMask,out matContornos,matCannyAndOthers,RetrievalModes.External, ContourApproximationModes.ApproxNone);
        Mat contornoPrincipal = matContornos[0];
        // contornoPrincipal = Cv2.FindContoursAsArray(matMask, RetrievalModes.External, ContourApproximationModes.ApproxNone).FirstOrDefault();
        Cv2.ConvexHull(contornoPrincipal,contornoPrincipal);
        contornoPrincipal /= escalaReduccionImagen;

        if (contornoPrincipal.Rows > 4)
        {
            hullLength = Cv2.ArcLength(contornoPrincipal, true);
            approxPolyStep = hullLength * 0.001d;
            for (double epsilon = approxPolyStep; epsilon < hullLength; epsilon += approxPolyStep)
            {
                Cv2.ApproxPolyDP(contornoPrincipal, matCannyAndOthers, epsilon, true);
                if (matCannyAndOthers.Rows == 4)
                {
                    matCannyAndOthers.ConvertTo(matCannyAndOthers,MatType.CV_32FC2);
                    matCannyAndOthers.GetArray(0,0,quadDetectado);
                    break;
                }
                else if (matCannyAndOthers.Rows < 4)
                {
                    Debug.LogError($"Me pase al reducir poligonos? epsilon {epsilon} nuevoPolyLen {matCannyAndOthers.Rows}");
                }
            }
        }
        else if (contornoPrincipal.Rows < 4) {
            return null;
        }

        var firstIndex = Enumerable.Range(0,4).Aggregate((a,b)=>quadDetectado[a].X+quadDetectado[a].Y>quadDetectado[b].X+quadDetectado[b].Y?b:a);
        
        (quadOrdenado[0],quadOrdenado[1],quadOrdenado[2],quadOrdenado[3]) =
           (quadDetectado[0+firstIndex],quadDetectado[(1+firstIndex)%4],quadDetectado[(2+firstIndex)%4],quadDetectado[(3+firstIndex)%4]) ;
            
            if (soloMarcar) {
                Cv2.Polylines(matImagenFuente,contornoPrincipal,true,new Scalar(255,200,0),20);
                return matImagenFuente;
            }

        //top left, top right, bottom right, bottom left
        (tl, tr, br, bl) = (quadOrdenado[0], quadOrdenado[1], quadOrdenado[2], quadOrdenado[3]);//okey... si esto anda...

        maxWidth = System.Math.Floor(System.Math.Max(tl.DistanceTo(tr), bl.DistanceTo(br)));
        maxHeight = System.Math.Floor(System.Math.Max(bl.DistanceTo(tl), br.DistanceTo(tr)));

        rectDestino[1].X = (float)maxWidth - 1;
        rectDestino[2].X = (float)maxWidth - 1;
        rectDestino[2].Y = (float)maxHeight - 1;
        rectDestino[3].Y = (float)maxHeight - 1;
        
        var matrizTransform = Cv2.GetPerspectiveTransform(quadOrdenado, rectDestino);
        
        Cv2.WarpPerspective(matImagenFuente, matSalida, matrizTransform, new Size((int)maxWidth, (int)maxHeight));

        if (texturesObserve != null) {
            texturesObserve[1] = OpenCvSharp.Unity.MatToTexture(matSalida,texturesObserve[1]);

            Cv2.CvtColor(matMask, matMask, ColorConversionCodes.GRAY2BGR);
            matCannyAndOthers *= escalaReduccionImagen;
           // Cv2.DrawContours(matMask,new []{matCannyAndOthers}, -1, new Scalar(100, 50, 255), 2);
            for (int i = 0; i < quadOrdenado.Length; i++)
            {
                matMask.Circle(quadOrdenado[i] * escalaReduccionImagen, 6, new Scalar(50, 250, 100), -1);
                for (int j = 0; j <= i; j++) matMask.Circle(quadOrdenado[i] * escalaReduccionImagen + new Point2f(6 * j + 9, i * 3), 3, new Scalar(50, 250, 100), -1);
            }

            texturesObserve[2] = OpenCvSharp.Unity.MatToTexture(matMask,texturesObserve[2]);
        }
        return matSalida;
    }
}
