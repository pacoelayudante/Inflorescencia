using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LibTessDotNet;

public class TestContorno : MonoBehaviour
{
    public Vector3 camPosFinal,camPosInicial;
    public Quaternion camRotFinal,camRotInicial;
    public ConfigExtraerContornoFlor configContorno;
    public ConfigAgentesDeFlor configAgentes;
    public Texture2D texturaDeContornos;
    public Texture2D TexturaDeContornos {
        get => TestRuntime.texturaExtraida?TestRuntime.texturaExtraida : texturaDeContornos;
    }
    ExtraerContornoFlor extractor;
    public MeshFilter tirarMeshAca;
    public float escala = 0.01f;
    public float altura = 10f;
    Mesh meshGenerada;
    public int cantSlices = 15;
    public int selectAgente = 0;
    public float escalaGizmo = 10f;
    public bool actualizarMesh = true;
    public float duracionCrecimiento = 5f;
    public AnimationCurve curvaAlturaFlor = AnimationCurve.EaseInOut(0,0,1,1);

    public float tamLadoBBox = 256;
    Vector2 offsetCentroContornos = Vector2.zero;
    float escalaContornosABBox = 1f;

    public List<Contorno> contornos;
    float tiempoCrecimiento;

    List<Vector3> verticesBase, verticesTotales;
    List<int> triangulos, triangulosTapa, triangulosLado;
    int cantVerticesBase;

    Vector3[] verticesVivos;
    Agente[] agentesVivos;

    public float distCerca => configAgentes._distCerca;// = 30f;
    public float multRepulsion => configAgentes._multRepulsion;// = 4f;
    public float multAtraccion => configAgentes._multAtraccion;// 0.0001f;
    public float expansion => configAgentes._expansion;// 0.1f;
    public float escalaFuerza => configAgentes._escalaFuerza;// 0.01f;
    public float decaeFuerza => configAgentes._decaeFuerza;// 1f;
    
    class Agente
    {
        public class AgenteComboIndexDist
        {
            public int idx;
            public float dist;
            public AgenteComboIndexDist(int idx, float dist)
            {
                this.idx = idx;
                this.dist = dist;
            }
            public AgenteComboIndexDist(int idx, float dist, float umbralDist)
            {
                this.dist = dist;
                this.idx = dist < umbralDist ? idx : -1;
            }
        }

        public Agente(Vector2 p, int[] cercanos = null, float[] cercanosD = null)
        {
            x = p.x;
            y = p.y;
            if (cercanos != null) this.cercanosIdx = cercanos;
            if (cercanosD != null) this.cercanosDist = cercanosD;
        }

        public Agente(Vector2 p, AgenteComboIndexDist[] cercanos = null)
        {
            x = p.x;
            y = p.y;
            if (cercanos != null)
            {
                this.cercanosIdx = cercanos.Select(c => c.idx).ToArray();
                this.cercanosDist = cercanos.Select(c => c.dist).ToArray();
            }
        }
        public float x, y;
        public float fX, fY;
        public int[] cercanosIdx;
        public float[] cercanosDist;
    }

    void Start()
    {
        Extraer();
        Meshificar();
        IniciarAgentes();
    }
    void OnDestroy()
    {
        extractor?.Dispose();
        if (meshGenerada) Destroy(meshGenerada);
    }

    [ContextMenu("Iniciar Agentes")]
    void IniciarAgentes()
    {
        if (contornos == null || contornos.Count == 0) Extraer();
        if (contornos == null || contornos.Count == 0) return;

        Vector2 min = contornos[0].vertices[0];//tengo que pasar todo esto a la parte de EXTRAER los contornos, aca no me sirve
        Vector2 max = contornos[0].vertices[0];
        
        List<Agente> listaAgentes = new List<Agente>();

        for (int c = 0, nC = contornos.Count; c < nC; c++)
        {
            var contorno = contornos[c];
            var vertices = contorno.vertices.Concat(contorno.contornosInternos.SelectMany(c => c.vertices)).ToArray();//en el mesh plano esto medio da igual

            var agentes = new List<Agente>();
            for (int i = 0, nV = vertices.Length; i < nV; i++)
            {
                var v = vertices[i];
                List<Agente.AgenteComboIndexDist> cercanos = new List<Agente.AgenteComboIndexDist>();
                for (int j = i + 1; j < nV; j++)
                {
                    var otro = vertices[j];
                    var d = Vector3.Distance(v, otro);
                    if (d < distCerca)
                    {
                        cercanos.Add(new Agente.AgenteComboIndexDist(j + listaAgentes.Count, d * escala));
                    }
                }
                agentes.Add(new Agente(v * escala, cercanos.ToArray()));

                if (v.x < min.x) min.x = v.x;
                if (v.x > max.x) max.x = v.x;
                if (v.y < min.y) min.y = v.y;
                if (v.y > max.y) max.y = v.y;
            }

            listaAgentes.AddRange(agentes);
        }

        escalaContornosABBox = tamLadoBBox*escala / Mathf.Max( max.x-min.x , max.y-min.y );

        var medianX = listaAgentes.OrderBy(agt=>agt.x).Skip(listaAgentes.Count/2).FirstOrDefault().x;
        var medianY = listaAgentes.OrderBy(agt=>agt.y).Skip(listaAgentes.Count/2).FirstOrDefault().y;

        offsetCentroContornos = new Vector2(-medianX,-medianY);//queda solo como registro

        for(int i=0,n=listaAgentes.Count;i<n;i++) {
            listaAgentes[i].x = (listaAgentes[i].x-medianX)*escalaContornosABBox;
            listaAgentes[i].y = (listaAgentes[i].y-medianY)*escalaContornosABBox;
        }
        agentesVivos = listaAgentes.ToArray();

        verticesVivos = agentesVivos.Select(agt => new Vector3(agt.x, agt.y, altura * escala)).ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        if (verticesVivos != null && verticesVivos.Length > 0 && agentesVivos != null && agentesVivos.Length > 0)
        {
            Gizmos.matrix = tirarMeshAca ? tirarMeshAca.transform.localToWorldMatrix : transform.localToWorldMatrix;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero,new Vector3(tamLadoBBox,tamLadoBBox,0)*escala);

            Gizmos.color = Color.red + Color.green * 0.5f;
            foreach (var ver in verticesVivos)
            {
                Gizmos.DrawSphere(Vector3.Scale(ver, new Vector3(1,1,0)), escala * escalaGizmo);
            }

            selectAgente = (selectAgente % verticesVivos.Length + verticesVivos.Length) % verticesVivos.Length;
            var vers = verticesVivos[selectAgente];
            vers.z = 0;
            var agt = agentesVivos[selectAgente];
            Gizmos.color = Color.red;
            Gizmos.DrawCube(vers, Vector3.one * escala * 0.3f + Vector3.forward);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(vers, escala * escalaGizmo * 1.001f);
            Gizmos.color = Color.blue;
            for (int i = 0, n = agt.cercanosIdx.Length; i < n; i++)
            {
                var v2 = verticesVivos[agt.cercanosIdx[i]];
                v2.z = 0;
                Gizmos.DrawSphere(v2, escala * escalaGizmo * 1.001f);

                Gizmos.DrawLine(vers, vers + (v2 - vers).normalized * agt.cercanosDist[i]);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape)) UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        if (!Input.GetMouseButton(0) && Input.touchCount==0) return;
        UpdateAgentes(Time.deltaTime);
    }
    void UpdateAgentes(float dt = 1f)
    {
        if (agentesVivos != null && verticesVivos != null)
        {
            if (tiempoCrecimiento < duracionCrecimiento) {
                UpdateRepulsion(dt);
                UpdateCercanos(dt);
                UpdateFuerzas(dt);
                tiempoCrecimiento += dt;
                // Camera.main.transform.position = Vector3.Lerp(camPosInicial,camPosFinal,curvaAlturaFlor.Evaluate(tiempoCrecimiento/duracionCrecimiento));
                // Camera.main.transform.rotation = Quaternion.Lerp(camRotInicial,camRotFinal,curvaAlturaFlor.Evaluate(tiempoCrecimiento/duracionCrecimiento));
                Camera.main.transform.position = Vector3.Lerp(camPosInicial,camPosFinal,(tiempoCrecimiento/duracionCrecimiento));
                Camera.main.transform.rotation = Quaternion.Lerp(camRotInicial,camRotFinal,(tiempoCrecimiento/duracionCrecimiento));
            }
            if (actualizarMesh && meshGenerada != null)
            {
                for (int v = 0, n = verticesVivos.Length; v < n; v++)
                {
                    verticesTotales[v] = verticesVivos[v];
                    
                    for (int s=0; s<=cantSlices; s++) {
                        var amt = s/(float)cantSlices;

                        // var vecGo = Vector3.Lerp(verticesBase[v],verticesVivos[v],curvaAlturaFlor.Evaluate( amt ));
                        // vecGo.z = amt;
                        var vecGo = Vector3.Lerp(verticesBase[v],verticesVivos[v],amt);
                        vecGo.z = curvaAlturaFlor.Evaluate( amt )*altura*tiempoCrecimiento/duracionCrecimiento;
                        verticesTotales[v+cantVerticesBase*(cantSlices)-s*cantVerticesBase] = vecGo;
                    }
                }
                meshGenerada.vertices = verticesTotales.ToArray();

                meshGenerada.RecalculateNormals();
                meshGenerada.RecalculateBounds();
                meshGenerada.RecalculateTangents();
            }
        }
    }
    void UpdateCercanos(float dt = 1f)
    {
        /*
        for (let othidx=0, n2=v.cerca.length; othidx<n2; ++othidx){
            const dbase = v.cerca[othidx].dSq;
            const other = v.cerca[othidx].vec;
            const xoff = (other.x-v.x);
            const yoff = (other.y-v.y);
            const sqDist = sqrt((xoff*xoff+yoff*yoff))-dbase;
                    
            tempVec.x = 0.0001*xoff*sqDist;
            tempVec.y = 0.0001*yoff*sqDist;
            other.fuerza.sub(tempVec);
            v.fuerza.add(tempVec);
            v.cerca[othidx].dSq += 0.1;
        }
        */
        for (int i = 0, n = agentesVivos.Length; i < n; i++)
        {
            var agente = agentesVivos[i];
            for (int iOtro = 0, nOtros = agente.cercanosIdx.Length; iOtro < nOtros; iOtro++)
            {
                var dBase = agente.cercanosDist[iOtro];
                var otro = agentesVivos[agente.cercanosIdx[iOtro]];
                var xoff = otro.x - agente.x;
                var yoff = otro.y - agente.y;
                var sqDist = Vector2.Distance(verticesVivos[i], verticesVivos[agente.cercanosIdx[iOtro]]) - dBase;

                xoff *= multAtraccion * sqDist * dt;
                yoff *= multAtraccion * sqDist * dt;
                otro.fX -= xoff;
                otro.fY -= yoff;
                agente.fX += xoff;
                agente.fY += yoff;
                agente.cercanosDist[iOtro] += expansion * dt;
            }
        }
    }
    void UpdateRepulsion(float dt = 1f)
    {
        /*
        for (let othidx=0, n2=v.lejos.length; othidx<n2; ++othidx){
                const other = v.lejos[othidx];
                const xoff = (other.x-v.x);
                const yoff = (other.y-v.y);
                const sqDist = xoff*xoff+yoff*yoff;

                tempVec.x = 4*xoff/sqDist;
                tempVec.y = 4*yoff/sqDist;
                other.fuerza.add(tempVec);
                v.fuerza.sub(tempVec);
              }
              */
        for (int i = 0, n = agentesVivos.Length; i < n; i++)
        {
            var agente = agentesVivos[i];
            for (int iOtro = i + 1; iOtro < n; iOtro++)
            {
                var otro = agentesVivos[iOtro];
                var xoff = otro.x - agente.x;
                var yoff = otro.y - agente.y;
                var sqDist = xoff * xoff + yoff * yoff;

                if (sqDist == 0f) continue;//ignorar posibles puntos solapados exactos (en realidad deberia limiparlos de la lista)

                xoff *= multRepulsion * dt / sqDist;
                yoff *= multRepulsion * dt / sqDist;
                otro.fX += xoff;
                otro.fY += yoff;
                agente.fX -= xoff;
                agente.fY -= yoff;
            }
        }
    }
    void UpdateFuerzas(float dt = 1f)
    {
        for (int i = 0, n = agentesVivos.Length; i < n; i++)
        {
            agentesVivos[i].x += agentesVivos[i].fX * dt * escalaFuerza;
            agentesVivos[i].fX *= decaeFuerza;
            verticesVivos[i].x = agentesVivos[i].x;

            agentesVivos[i].y += agentesVivos[i].fY * dt * escalaFuerza;
            agentesVivos[i].fY *= decaeFuerza;
            verticesVivos[i].y = agentesVivos[i].y;

            verticesVivos[i].z += dt * altura;
        }
    }

    [ContextMenu("Reiniciar")]
    public void Reiniciar() {
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
                verticesBase.AddRange(contorno.vertices.Select(v => v * escala));

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

                    verticesBase.AddRange(contInterno.vertices.Select(v => v * escala));
                }

                teselasPorContorno[c].Tessellate(WindingRule.Positive, ElementType.Polygons, 3, VertexCombine);
                
                triangulosTapa.AddRange(
                    teselasPorContorno[c].Elements.Select(ind => ((ContVertIdx)teselasPorContorno[c].Vertices[ind].Data).vidx)
                );
            }

            triangulos.AddRange(triangulosTapa);
            var offaltura = Vector3.forward * altura * escala;

            verticesTotales = new List<Vector3>();
            verticesTotales.AddRange(verticesBase.Select(v => v + offaltura));
            
            if (cantSlices < 1) cantSlices = 1;
            int offsetVertex = 0;
            for (float i = cantSlices-1f;i >= 0f; i--)
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
