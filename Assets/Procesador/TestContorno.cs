using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LibTessDotNet;

public class TestContorno : MonoBehaviour
{
    public bool autoSimulate = true;
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

    public AnimatorUpdateMode updateMode = AnimatorUpdateMode.Normal;

    public float tamLadoBBox = 256;
    public Vector3 offsetCentroContornosEscalado = Vector3.zero;
    // float escalaContornosABBox = 1f;

    public List<Contorno> contornos;
    float tiempoCrecimiento;

    List<Vector3> verticesBase, verticesTotales;
    List<List<Vector3>> progresionDeVertices = new List<List<Vector3>>();
    List<float> tiemposProgreso = new List<float>();
    List<int> triangulos, triangulosTapa, triangulosLado;
    int cantVerticesBase;

    bool meshNecesitaActualizar;

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
        progresionDeVertices.Clear();
        tiemposProgreso.Clear();
        progresionDeVertices.Add(verticesBase);
        tiemposProgreso.Add(0f);
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
                
                // Gizmos.DrawSphere(Vector3.Scale(ver, new Vector3(1, 1, 1/escala)), escalaGizmo);
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

        if (autoSimulate || Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            if (updateMode == AnimatorUpdateMode.Normal) UpdateAgentes(Time.deltaTime);
            else if (updateMode == AnimatorUpdateMode.UnscaledTime) UpdateAgentes(1f / 60f);
        }

        if (meshNecesitaActualizar)
        {
            meshNecesitaActualizar = false;
            ActualizarMeshGenerada();
        }
    }
    void FixedUpdate()
    {
        if (autoSimulate || Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            if (updateMode == AnimatorUpdateMode.AnimatePhysics) UpdateAgentes(Time.fixedDeltaTime);
        }
    }
    void UpdateAgentes(float dt = 1f)
    {
        if (controlAgentes != null)
        {
            if (tiempoCrecimiento < duracionCrecimiento)
            {

                controlAgentes.UpdateAgentes(dt);
                tiempoCrecimiento += dt;
                meshNecesitaActualizar = true;
                for (int i = 0, n = controlAgentes.Length; i < n; i++)
                {
                    verticesVivos[i].x = agentesVivos[i].X;
                    verticesVivos[i].y = agentesVivos[i].Y;
                    verticesVivos[i].z = tiempoCrecimiento * altura;
                }

                //aca tengo que fijarme si el crecimiento supero un umbral como para fijar el estado actual del contorno
                var cantSlicesEsparados = cantSlices * curvaAlturaFlor.Evaluate( tiempoCrecimiento / duracionCrecimiento );
                if (progresionDeVertices.Count < cantSlicesEsparados)
                {
                    // Debug.Log($"nueva foto de slice  {progresionDeVertices.Count} , progreso = {tiempoCrecimiento / duracionCrecimiento} - ({cantSlices * tiempoCrecimiento / duracionCrecimiento})");
                    progresionDeVertices.Add(verticesVivos.ToList());// esto del ToList es porque tengo Linq activado, mi idea es crear una copa del actual
                    tiemposProgreso.Add(tiempoCrecimiento/duracionCrecimiento);
                }

                var tNorm = (tiempoCrecimiento / duracionCrecimiento);
                Camera.main.transform.position = Vector3.Lerp(camPosInicial, camPosFinal, tNorm * tNorm);
                Camera.main.transform.rotation = Quaternion.Lerp(camRotInicial, camRotFinal, tNorm);
            }
        }
    }
    void ActualizarMeshGenerada()
    {
        if (meshGenerada != null)
        {
            for (int v = 0, n = verticesVivos.Length; v < n; v++)
            {
                verticesTotales[v] = verticesVivos[v];

                for (int s = 0; s <= cantSlices; s++)
                {
                    var amt = s / (float)cantSlices;
                    //aca tengo que decidir si usar estados guardados o el estado vivo
                    if (s < progresionDeVertices.Count)
                    {
                        //hay estado progresivo
                        var vecGo = progresionDeVertices[s][v];
                        // vecGo.z = curvaAlturaFlor.Evaluate(amt) * altura * tiempoCrecimiento / duracionCrecimiento;
                        vecGo.z = tiemposProgreso[s] * altura ;
                        verticesTotales[v + cantVerticesBase * (cantSlices) - s * cantVerticesBase] = vecGo;
                    }
                    else
                    {//lerpear con el ultimo, slice los vertices vivos
                     // ok los vertices base tiene que pasar a ser la anterior figura guardada
                     // los vivos la "actual figura"
                     // y los vivos ahora seran nomas el ultimo slice? o mas bien todos los slices que no tengan 
                     // una forma guardada
                     var ultiIndex = progresionDeVertices.Count-1;
                     amt = Mathf.InverseLerp(ultiIndex/(float)cantSlices,1f,amt);
                        var vecGo = Vector3.Lerp(progresionDeVertices[ultiIndex][v], verticesVivos[v], amt);
                        // vecGo.z = curvaAlturaFlor.Evaluate(amt) * altura * tiempoCrecimiento / duracionCrecimiento;
                        vecGo.z =  altura * Mathf.Lerp(tiemposProgreso[ultiIndex],tiempoCrecimiento / duracionCrecimiento,amt);
                        verticesTotales[v + cantVerticesBase * (cantSlices) - s * cantVerticesBase] = vecGo;
                    }
                }
            }
            meshGenerada.vertices = verticesTotales.ToArray();

            meshGenerada.RecalculateNormals();
            meshGenerada.RecalculateBounds();
            meshGenerada.RecalculateTangents();
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
            var offaltura = Vector3.forward * altura * 0;

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
