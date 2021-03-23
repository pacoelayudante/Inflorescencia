using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.Linq;

[System.Serializable]
public class ExtraerContornoFlor : System.IDisposable
{
    public ConfigExtraerContornoFlor config;
    public int Border => config._border;
    public float SizeLimit => config._sizeLimit;
    public float Threshold => config._threshold;
    public ThresholdTypes ThreshType => config._threshType;
    public bool UsarCanny => config._usarCanny;
    public float CannyBajo => config._cannyBajo;
    public float CannyAlto => config._cannyAlto;
    public int ApertureCanny => config._apertureCanny;
    public bool CannyL2 => config._cannyL2;

    public ExtraerContornoFlor(ConfigExtraerContornoFlor config, bool autoIniciar = true)
    {
        this.config = config;
        if (autoIniciar) Iniciar();
    }

    Mat matImagenLowRes, matBin;

    void Iniciar()
    {
        if (matImagenLowRes == null) matImagenLowRes = new Mat();
        if (matBin == null) matBin = new Mat();
    }

    public void Dispose()
    {
        matImagenLowRes?.Dispose();
        matBin?.Dispose();
    }

    public List<Contorno> Procesar(Mat matImagenFuente, Texture2D[] texturesObserve = null)
    {
        var contornos = new List<Contorno>();

        Iniciar();

        var escalaReduccionImagen = 1f;
        if (matImagenFuente.Width > SizeLimit || matImagenFuente.Height > SizeLimit)
        {
            escalaReduccionImagen = Mathf.Min(SizeLimit / matImagenFuente.Width, SizeLimit / matImagenFuente.Height);
            Cv2.Resize(matImagenFuente, matImagenLowRes, new Size(matImagenFuente.Width * escalaReduccionImagen, matImagenFuente.Height * escalaReduccionImagen));
        }
        else Cv2.Resize(matImagenFuente, matImagenLowRes, matImagenFuente.Size());

        Cv2.CvtColor(matImagenLowRes, matImagenLowRes, ColorConversionCodes.RGB2GRAY);
        if (UsarCanny) Cv2.Canny(matImagenLowRes, matBin, CannyBajo, CannyAlto, ApertureCanny, CannyL2);
        else Cv2.Threshold(matImagenLowRes, matBin, Threshold, 255, ThreshType);
        //borde extra por las dudas si hay ruido en lso bordes
        Cv2.Rectangle(matBin,new Point(0,0), new Point(matBin.Width,matBin.Height),new Scalar(0),Border);
        if (texturesObserve != null) texturesObserve[0] = OpenCvSharp.Unity.MatToTexture(matBin, texturesObserve[0]);

        Point[][] puntos;
        HierarchyIndex[] jerarquia;
        Cv2.FindContours(matBin, out puntos, out jerarquia, RetrievalModes.Tree, ContourApproximationModes.ApproxTC89KCOS);

        if (jerarquia.Length == 0) return null;
        contornos = OrdenarContornosEvitandoCanny(puntos,jerarquia,0,contornos);
        Debug.Log(contornos.Count);
        /*if (jerarquia.Length == 0) return null;
        var nexts = 0;
        while (nexts != -1) {
            var j = jerarquia[nexts];
            //el primer piso lo quiero ignorar, pero quiero hacer una lista de todos
            //los contornos que estan abajo de este primer piso
            List<int> nietos = new List<int>();
            if (j.Child != -1) {
                var ch = j.Child;
                while (ch != -1) {
                    var chDelCh = jerarquia[ch].Child;
                    while (chDelCh != -1) {
                        nietos.Add(chDelCh);
                        chDelCh = jerarquia[chDelCh].Next;//aca habriaque ver si hay aun mas descendencia! ojo!
                    }
                    ch = jerarquia[ch].Next;
                }

            }

            contornos.Add(new Contorno(){
                color = Color.HSVToRGB(Random.value,1,1),
                contornosInternos=nietos.Select(n=>new Contorno(){vertices=puntos[n].Select(p => new Vector2(p.X, p.Y)).ToArray()}).ToArray(),
                vertices=puntos[nexts].Select(p => new Vector2(p.X, p.Y)).ToArray()
            });
            nexts = j.Next;
        }*/
        
        // jerarquia.Select((j, idx) => new { jerarq = j, verts = puntos[idx] }).Where(j => j.jerarq.Parent == -1)
        //     .Select(j => new Contorno()
        //     {
        //         vertices = j.verts.Select(p => new Vector2(p.X, p.Y)).ToArray(),
        //         contornosInternos = new[] { new Contorno() { vertices = puntos[j.jerarq.Child].Select(p => new Vector2(p.X, p.Y)).ToArray() } }
        //     }
        //     );

        if (texturesObserve != null)
        {
            Cv2.CvtColor(matImagenLowRes, matImagenLowRes, ColorConversionCodes.GRAY2RGB);
            //Cv2.DrawContours(matImagenLowRes, puntos, -1, new Scalar(0, 100, 255), Mathf.CeilToInt(Mathf.Min(matImagenLowRes.Width * 0.005f, 1)));
            foreach(var cont in contornos) {
                var dibujar = new[]{cont.vertices.Select(v=>new Point(v.x,v.y))};
                Cv2.DrawContours(matImagenLowRes, dibujar, -1, new Scalar(cont.color.b*255, cont.color.g*255, cont.color.r*255), Mathf.CeilToInt(Mathf.Min(matImagenLowRes.Width * 0.005f, 1)));
                
                dibujar = cont.contornosInternos.Select(c=>c.vertices.Select(v=>new Point(v.x,v.y))).ToArray();
                Cv2.DrawContours(matImagenLowRes, dibujar, -1, new Scalar(cont.color.b*100, cont.color.g*100, cont.color.r*100), Mathf.CeilToInt(Mathf.Min(matImagenLowRes.Width * 0.005f, 1)));
            }
            texturesObserve[1] = OpenCvSharp.Unity.MatToTexture(matImagenLowRes, texturesObserve[1]);
        }

        return contornos;
    }

    List<Contorno> OrdenarContornosEvitandoCanny(Point[][] puntos, HierarchyIndex[] jerarquia, int inicial, List<Contorno> lista=null) {
        if (lista==null) lista = new List<Contorno>();

        List<HierarchyIndex> posiblesSubcontornos = new List<HierarchyIndex>();

        var nexts = inicial;
        while (nexts != -1) {
            var j = jerarquia[nexts];
            //el primer piso lo quiero ignorar, pero quiero hacer una lista de todos
            //los contornos que estan abajo de este primer piso
            //edit: no quiero los nietos, sino los bisnietos (porque los nietos son los fake pero su orden esta invertido)
            List<int> bisnietos = new List<int>();
            if (j.Child != -1) {
                var ch = j.Child;
                while (ch != -1) {
                    var chDelCh = jerarquia[ch].Child;
                    while (chDelCh != -1) {
                        var bisnietoIdx = jerarquia[chDelCh].Child;
                        while (bisnietoIdx != -1) {
                            bisnietos.Add(bisnietoIdx);
                            if (jerarquia[bisnietoIdx].Child!=-1) lista = OrdenarContornosEvitandoCanny(puntos,jerarquia,jerarquia[bisnietoIdx].Child,lista);
                            bisnietoIdx = jerarquia[bisnietoIdx].Next;
                        }
                        chDelCh = jerarquia[chDelCh].Next;//aca habriaque ver si hay aun mas descendencia! ojo!
                    }
                    ch = jerarquia[ch].Next;
                }

            }

            lista.Add(new Contorno(){
                color = Color.HSVToRGB(Random.value,1,1),
                contornosInternos=bisnietos.Select(n=>new Contorno(){color=Color.magenta,vertices=puntos[n].Select(p => new Vector2(p.X, p.Y)).ToArray()}).ToArray(),
                vertices=puntos[nexts].Select(p => new Vector2(p.X, p.Y)).ToArray()
            });
            nexts = j.Next;
        }

        return lista;
    }
}
