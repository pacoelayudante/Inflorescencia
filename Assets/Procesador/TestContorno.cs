using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LibTessDotNet;

public class TestContorno : MonoBehaviour
{
    public Vector3 camPosFinal, camPosInicial;
    public Quaternion camRotFinal, camRotInicial;
    public ConfigExtraerContornoFlor configContorno;
    public ConfigAgentesDeFlor configAgentes;
    public Texture2D texturaDeContornos;
    public Texture2D TexturaDeContornos
    {
        get => ControlTapa.imagenRecortada ? ControlTapa.imagenRecortada : texturaDeContornos;
    }
    ExtraerContornoFlor extractor;
    public MeshFilter tirarMeshAca;
    public float escala = 0.01f;//pensar en que va a ser obsoleto, ahora vamos a usar lo de escalaContornos mas bien
    public float altura = 10f;
    Mesh meshGenerada;
    public int cantSlices = 15;
    public int selectAgente = 0;
    public float escalaGizmo = 10f;
    public bool actualizarMesh = true;
    public float duracionCrecimiento = 5f;
    public AnimationCurve curvaAlturaFlor = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public float tamLadoBBox = 256;
    public Vector3 offsetCentroContornosEscalado = Vector3.zero;
    // float escalaContornosABBox = 1f;

    public List<Contorno> contornos;
    float tiempoCrecimiento;

    List<Vector3> verticesBase, verticesTotales;
    List<int> triangulos, triangulosTapa, triangulosLado;
    int cantVerticesBase;

    ControlAgentes controlAgentes;
    Vector3[] verticesVivos;
    ControlAgentes.Agente[] agentesVivos => controlAgentes == null ? null : controlAgentes.agentesVivos;

    void Start()
    {
        if (ControlTapa.imagenRecortada) texturaDeContornos = ControlTapa.imagenRecortada;
        Extraer();
        NormalizarContornos();
        Meshificar();
        IniciarAgentes();
    }
    void OnDestroy()
    {
        extractor?.Dispose();
        if (meshGenerada) Destroy(meshGenerada);
    }

    [ContextMenu("Normalizar Contornos")]
    void NormalizarContornos()
    {
        if (contornos == null || contornos.Count == 0) Extraer();

        Rect bbox = new Rect(contornos[0].vertices[0].x, contornos[0].vertices[0].y, 0, 0);
        for (int i = 0, n = contornos.Count; i < n; i++)
        {
            for (int v = 0, n2 = contornos[i].vertices.Length; v < n2; v++)
            {
                if (contornos[i].vertices[v].x < bbox.xMin) bbox.xMin = contornos[i].vertices[v].x;
                if (contornos[i].vertices[v].y < bbox.yMin) bbox.yMin = contornos[i].vertices[v].y;
                if (contornos[i].vertices[v].x > bbox.xMax) bbox.xMax = contornos[i].vertices[v].x;
                if (contornos[i].vertices[v].y > bbox.yMax) bbox.yMax = contornos[i].vertices[v].y;
            }
        }
        escala = tamLadoBBox / Mathf.Max(bbox.width, bbox.height);

        var verts = contornos.SelectMany(cont => cont.vertices);
        // var offx = -verts.OrderBy(v => v.x).Skip(verts.Count()/2).FirstOrDefault().x;
        // var offy = -verts.OrderBy(v => v.y).Skip(verts.Count()/2).FirstOrDefault().y;
        var offx = -bbox.center.x;
        var offy = -bbox.center.y;

        offsetCentroContornosEscalado = new Vector3(offx, offy, 0) * escala;
    }

    [ContextMenu("Iniciar Agentes")]
    void IniciarAgentes()
    {
        if (contornos == null || contornos.Count == 0) Extraer();
        controlAgentes = new ControlAgentes(configAgentes, contornos, escala, offsetCentroContornosEscalado);
        verticesVivos = agentesVivos.Select(agt => new Vector3(agt.X, agt.Y, 0)).ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.matrix = tirarMeshAca ? tirarMeshAca.transform.localToWorldMatrix : transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(tamLadoBBox, tamLadoBBox, 0));

        if (verticesVivos != null && verticesVivos.Length > 0 && agentesVivos != null && agentesVivos.Length > 0)
        {
            Gizmos.color = Color.red + Color.green * 0.5f;
            foreach (var ver in verticesVivos)
            {
                Gizmos.DrawSphere(Vector3.Scale(ver, new Vector3(1, 1, 0)), escalaGizmo);
            }

            selectAgente = (selectAgente % verticesVivos.Length + verticesVivos.Length) % verticesVivos.Length;
            var vers = verticesVivos[selectAgente];
            vers.z = 0;
            var agt = agentesVivos[selectAgente];
            Gizmos.color = Color.red;
            Gizmos.DrawCube(vers, Vector3.one * 0.03f + Vector3.forward);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(vers, escalaGizmo * 1.001f);
            Gizmos.color = Color.blue;
            for (int i = 0, n = agt.cercanosIdx.Length; i < n; i++)
            {
                var v2 = verticesVivos[agt.cercanosIdx[i]];
                v2.z = 0;
                Gizmos.DrawSphere(v2, escalaGizmo * 1.001f);

                Gizmos.DrawLine(vers, vers + (v2 - vers).normalized * agt.cercanosDist[i]);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape)) UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        if (!Input.GetMouseButton(0) && Input.touchCount == 0) return;
        UpdateAgentes(Time.deltaTime);
    }
    void UpdateAgentes(float dt = 1f)
    {
        if (controlAgentes != null)
        {
            if (tiempoCrecimiento < duracionCrecimiento)
            {

                controlAgentes.UpdateAgentes(dt);
                tiempoCrecimiento += dt;
                for (int i = 0, n = controlAgentes.Length; i < n; i++)
                {
                    verticesVivos[i].x = agentesVivos[i].X;
                    verticesVivos[i].y = agentesVivos[i].Y;
                    verticesVivos[i].z = tiempoCrecimiento * altura;
                }

                // Camera.main.transform.position = Vector3.Lerp(camPosInicial,camPosFinal,curvaAlturaFlor.Evaluate(tiempoCrecimiento/duracionCrecimiento));
                // Camera.main.transform.rotation = Quaternion.Lerp(camRotInicial,camRotFinal,curvaAlturaFlor.Evaluate(tiempoCrecimiento/duracionCrecimiento));
                Camera.main.transform.position = Vector3.Lerp(camPosInicial, camPosFinal, (tiempoCrecimiento / duracionCrecimiento));
                Camera.main.transform.rotation = Quaternion.Lerp(camRotInicial, camRotFinal, (tiempoCrecimiento / duracionCrecimiento));
            }
            if (actualizarMesh && meshGenerada != null)
            {
                for (int v = 0, n = verticesVivos.Length; v < n; v++)
                {
                    verticesTotales[v] = verticesVivos[v];

                    for (int s = 0; s <= cantSlices; s++)
                    {
                        var amt = s / (float)cantSlices;

                        // var vecGo = Vector3.Lerp(verticesBase[v],verticesVivos[v],curvaAlturaFlor.Evaluate( amt ));
                        // vecGo.z = amt;
                        var vecGo = Vector3.Lerp(verticesBase[v], verticesVivos[v], amt);
                        vecGo.z = curvaAlturaFlor.Evaluate(amt) * altura * tiempoCrecimiento / duracionCrecimiento;
                        verticesTotales[v + cantVerticesBase * (cantSlices) - s * cantVerticesBase] = vecGo;
                    }
                }
                meshGenerada.vertices = verticesTotales.ToArray();

                meshGenerada.RecalculateNormals();
                meshGenerada.RecalculateBounds();
                meshGenerada.RecalculateTangents();
            }
        }
    }

    [ContextMenu("Reiniciar")]
    public void Reiniciar()
    {
        IniciarAgentes();
        tiempoCrecimiento = 0;
        Meshificar();
    }

    [ContextMenu("Extraer")]
    public void Extraer()
    {
        if (configContorno == null) return;
        if (extractor != null && extractor.config == null)
        {
            extractor.Dispose();
            extractor = null;
        }
        var extractorTemp = extractor;
        if (extractorTemp == null) extractorTemp = new ExtraerContornoFlor(configContorno);
        var matParaProcesar = OpenCvSharp.Unity.TextureToMat(TexturaDeContornos);
        contornos = extractorTemp.Procesar(matParaProcesar);
        if (extractor == null) extractorTemp.Dispose();
    }

    [ContextMenu("Meshificar")]
    public void Meshificar()
    {
        if (contornos != null && tirarMeshAca)
        {
            if (meshGenerada == null) meshGenerada = new Mesh();
            else
            {
                meshGenerada.Clear();
            }
            tirarMeshAca.sharedMesh = meshGenerada;

            verticesBase = new List<Vector3>();
            triangulos = new List<int>();
            triangulosLado = new List<int>();
            triangulosTapa = new List<int>();
            Tess[] teselasPorContorno = new Tess[contornos.Count];
            cantVerticesBase = contornos.Sum(c => c.vertices.Length + c.contornosInternos.Sum(ci => ci.vertices.Length));

            for (int c = 0, n = contornos.Count; c < n; c++)
            {
                var contorno = contornos[c];

                teselasPorContorno[c] = new Tess();
                teselasPorContorno[c].AddContour(contorno.vertices.Select((v, idx) => new ContourVertex(new Vec3(v.x, v.y, 0), new ContVertIdx(contorno, idx + verticesBase.Count))).ToArray(), ContourOrientation.Clockwise);

                for (int v = 0, nV = contorno.vertices.Length; v < nV; v++)
                {
                    int v2 = (v + 1) % nV;
                    triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                    triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                    triangulosLado.Add(v + verticesBase.Count);//este arriba

                    triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                    triangulosLado.Add(v2 + verticesBase.Count + cantVerticesBase);//siguiente abajo
                    triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                }
                verticesBase.AddRange(contorno.vertices.Select(v => v * escala + offsetCentroContornosEscalado));

                for (int cInt = 0, nInt = contorno.contornosInternos.Length; cInt < nInt; cInt++)
                {
                    var contInterno = contorno.contornosInternos[cInt];
                    teselasPorContorno[c].AddContour(contInterno.vertices.Select((v, idx) => new ContourVertex(new Vec3(v.x, v.y, 0), new ContVertIdx(contInterno, idx + verticesBase.Count))).ToArray(), ContourOrientation.CounterClockwise);

                    for (int v = 0, nV = contInterno.vertices.Length; v < nV; v++)
                    {
                        int v2 = (v + 1) % nV;
                        triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                        triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                        triangulosLado.Add(v + verticesBase.Count);//este arriba

                        triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                        triangulosLado.Add(v2 + verticesBase.Count + cantVerticesBase);//siguiente abajo
                        triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                    }

                    verticesBase.AddRange(contInterno.vertices.Select(v => v * escala + offsetCentroContornosEscalado));
                }

                teselasPorContorno[c].Tessellate(WindingRule.Positive, ElementType.Polygons, 3, VertexCombine);

                triangulosTapa.AddRange(
                    teselasPorContorno[c].Elements.Select(ind => ((ContVertIdx)teselasPorContorno[c].Vertices[ind].Data).vidx)
                );
            }

            triangulos.AddRange(triangulosTapa);
            var offaltura = Vector3.forward * altura*0;

            verticesTotales = new List<Vector3>();
            verticesTotales.AddRange(verticesBase.Select(v => v + offaltura));

            if (cantSlices < 1) cantSlices = 1;
            int offsetVertex = 0;
            for (float i = cantSlices - 1f; i >= 0f; i--)
            {
                verticesTotales.AddRange(verticesBase.Select(v => v + offaltura * i / cantSlices));
                triangulos.AddRange(triangulosLado.Select(idx => idx + offsetVertex));
                offsetVertex += cantVerticesBase;
            }

            meshGenerada.vertices = verticesTotales.ToArray();
            meshGenerada.triangles = triangulos.ToArray();
            meshGenerada.RecalculateNormals();
            meshGenerada.RecalculateBounds();
            meshGenerada.RecalculateTangents();
        }
    }

    class ContVertIdx
    {
        public Contorno contorno;
        public int vidx;
        public ContVertIdx(Contorno contorno, int vidx)
        {
            this.contorno = contorno;
            this.vidx = vidx;
        }
    }
    private static object VertexCombine(LibTessDotNet.Vec3 position, object[] data, float[] weights)
    {
        var resultado = data[0];
        float wMayor = weights[0];
        for (int i = 1, n = weights.Length; i < n; i++)
        {
            if (weights[i] > wMayor)
            {
                resultado = data[i];
                wMayor = weights[i];
            }
        }
        return resultado;
    }
}
