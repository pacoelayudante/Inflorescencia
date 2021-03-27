using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LibTessDotNet;

public class TestContorno : MonoBehaviour
{
    public ConfigExtraerContornoFlor config;
    public Texture2D texturaDeContornos;
    ExtraerContornoFlor extractor;
    public MeshFilter tirarMeshAca;
    public float escala = 0.01f;
    public float altura = 10f;
    Mesh meshGenerada;
    public int selectAgente = 0;
    public bool actualizarMesh = true;

    public List<Contorno> contornos;

    List<Vector3> verticesBase,verticesTotales;
    List<int> triangulos,triangulosTapa,triangulosLado;
    int cantVerticesBase;

    Vector3[] verticesVivos;
    Agente[] agentesVivos;

    public float distCerca = 30f;
    public float multRepulsion = 4f;
    public float multAtraccion = 0.0001f;
    public float expansion = 0.1f;
    public float escalaFuerza = 0.01f;
    public float decaeFuerza = 1f;
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
        //extractor = new ExtraerContornoFlor(config);
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
        //var vertCountEnUnaCapa = contornos.Sum(cont => cont.vertices.Length + cont.contornosInternos.Sum(subc => subc.vertices.Length));

        //int offsetIndexes = 0;
        List<Agente> listaAgentes = new List<Agente>();

        for (int c = 0, nC = contornos.Count; c < nC; c++)//foreach (var contorno in contornos)
        {
            var contorno = contornos[c];
            var vertices = contorno.vertices.Concat(contorno.contornosInternos.SelectMany(c => c.vertices)).ToArray();//en el mesh plano esto medio da igual
            // var vcount = vertices.Count();

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
                        cercanos.Add(new Agente.AgenteComboIndexDist(j + listaAgentes.Count, d*escala));
                    }
                }
                agentes.Add(new Agente(v * escala, cercanos.ToArray()));
            }
            /*
                        var agentes = vertices.Select((v, idx) =>
                            new Agente(v * escala, vertices.Skip(idx + 1)
                                // .Select((otro,idxOtro)=>idx+1+idxOtro+offsetIndexes)
                                //.Select((otro,idxOtro)=>Vector3.Distance(v,otro)<distCerca?(idx+1+idxOtro+offsetIndexes):-1)
                                .Select((otro, idxOtro) => new Agente.AgenteComboIndexDist((idx + 1 + idxOtro + offsetIndexes), Vector3.Distance(v, otro), distCerca))
                                .Where(otro => otro.idx != -1).ToArray())
                        ).ToArray();*/

            listaAgentes.AddRange(agentes);
            // offsetIndexes += vcount;
        }

        agentesVivos = listaAgentes.ToArray();
        
        verticesVivos = listaAgentes.Select(agt => new Vector3(agt.x, agt.y, altura*escala)).ToArray();
        
        // for(int i=0,n=verticesVivos.Length;i<n;i++) {
        //     for (int i2=0,n2=agt.cercanosIdx.Length; i2<n2;i2++) {
        //         agentesVivos[i].cercanosDist[i2] = Vector3.Distance(verticesVivos[i],verticesVivos[i2]);
        //     }
        // }

    }

    private void OnDrawGizmosSelected()
    {
        if (verticesVivos != null && verticesVivos.Length > 0 && agentesVivos != null && agentesVivos.Length > 0)
        {
            Gizmos.matrix = tirarMeshAca ? tirarMeshAca.transform.localToWorldMatrix : transform.localToWorldMatrix;
            Gizmos.color = Color.red + Color.green * 0.5f;
            foreach (var ver in verticesVivos)
            {
                Gizmos.DrawSphere(ver, escala * 0.5f);
            }

            selectAgente = (selectAgente % verticesVivos.Length + verticesVivos.Length) % verticesVivos.Length;
            var vers = verticesVivos[selectAgente];
            var agt = agentesVivos[selectAgente];
            Gizmos.color = Color.red;
            Gizmos.DrawCube(vers, Vector3.one * escala * 0.3f + Vector3.forward);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(vers, escala * 0.55f);
            Gizmos.color = Color.blue;
            for (int i = 0, n = agt.cercanosIdx.Length; i < n; i++)
            {
                var v2 = verticesVivos[agt.cercanosIdx[i]];
                Gizmos.DrawSphere(v2, escala * 0.55f);

                Gizmos.DrawLine( vers, vers+ (v2-vers).normalized*agt.cercanosDist[i] );
            }
        }
    }

    void Update()
    {
        UpdateAgentes(Time.deltaTime);
    }
    void UpdateAgentes(float dt = 1f)
    {
        if (agentesVivos != null && verticesVivos != null)
        {
            UpdateRepulsion(dt);
            UpdateCercanos(dt);
            UpdateFuerzas(dt);
            if (actualizarMesh && meshGenerada != null)
            {
                for (int v=0,n=verticesVivos.Length; v<n; v++) {
                    verticesTotales[v] = verticesVivos[v];
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
            for (int iOtro = i+1; iOtro < n; iOtro++)
            {
                var otro = agentesVivos[iOtro];
                var xoff = otro.x - agente.x;
                var yoff = otro.y - agente.y;
                var sqDist = xoff*xoff+yoff*yoff;

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
            agentesVivos[i].fX*=decaeFuerza;
            verticesVivos[i].x = agentesVivos[i].x;

            agentesVivos[i].y += agentesVivos[i].fY * dt * escalaFuerza;
            agentesVivos[i].fY*=decaeFuerza;
            verticesVivos[i].y = agentesVivos[i].y;

            verticesVivos[i].z += dt*altura;
        }
    }

    [ContextMenu("Extraer")]
    public void Extraer()
    {
        if (config == null) return;
        if (extractor != null && extractor.config == null)
        {
            extractor.Dispose();
            extractor = null;
        }
        var extractorTemp = extractor;
        if (extractorTemp == null) extractorTemp = new ExtraerContornoFlor(config);
        var matParaProcesar = OpenCvSharp.Unity.TextureToMat(texturaDeContornos);
        contornos = extractorTemp.Procesar(matParaProcesar);
        if (extractor == null) extractorTemp.Dispose();
    }

    [ContextMenu("Meshificar")]
    public void Meshificar() {
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
            cantVerticesBase = contornos.Sum( c=>c.vertices.Length+c.contornosInternos.Sum(ci=>ci.vertices.Length) );

            for (int c=0,n=contornos.Count; c<n; c++) {
                var contorno = contornos[c];

                teselasPorContorno[c] = new Tess();
                teselasPorContorno[c].AddContour(contorno.vertices.Select((v,idx) => new ContourVertex(new Vec3(v.x, v.y, 0),new ContVertIdx(contorno,idx+verticesBase.Count))).ToArray(), ContourOrientation.Clockwise);

                for (int v=0,nV=contorno.vertices.Length; v<nV; v++) {
                    int v2 = (v+1)%nV;
                    triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                    triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                    triangulosLado.Add(v + verticesBase.Count);//este arriba

                    triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                    triangulosLado.Add(v2 + verticesBase.Count + cantVerticesBase);//siguiente abajo
                    triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                }
                verticesBase.AddRange(contorno.vertices.Select(v=>v*escala));

                for (int cInt=0,nInt=contorno.contornosInternos.Length; cInt<nInt; cInt++) {
                    var contInterno = contorno.contornosInternos[cInt];
                    teselasPorContorno[c].AddContour(contInterno.vertices.Select((v,idx) => new ContourVertex(new Vec3(v.x, v.y, 0),new ContVertIdx(contInterno,idx+verticesBase.Count))).ToArray(), ContourOrientation.CounterClockwise);

                    for (int v=0,nV=contInterno.vertices.Length; v<nV; v++) {
                        int v2 = (v+1)%nV;
                        triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                        triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                        triangulosLado.Add(v + verticesBase.Count);//este arriba

                        triangulosLado.Add(v2 + verticesBase.Count);//siguiente arriba
                        triangulosLado.Add(v2 + verticesBase.Count + cantVerticesBase);//siguiente abajo
                        triangulosLado.Add(v + verticesBase.Count + cantVerticesBase);//este abajo
                    }

                    verticesBase.AddRange(contInterno.vertices.Select(v=>v*escala));
                }

                teselasPorContorno[c].Tessellate(WindingRule.Positive, ElementType.Polygons, 3, VertexCombine);

                // var verticesAgrupados = teselasPorContorno[c].Vertices.Select(v=>(ContVertIdx)v.Data).GroupBy(d=>d.contorno)
                //     ;// .ToDictionary(v=>v.Key);


                // foreach (var cont in verticesAgrupados) {
                //     cont.Key.vertices = cont.OrderBy(d=>d.vidx).Select(v=> cont.Key.vertices[v.vidx] ).ToArray();
                //     cont.
                // }

                triangulosTapa.AddRange(
                    teselasPorContorno[c].Elements.Select(ind=>((ContVertIdx)teselasPorContorno[c].Vertices[ind].Data).vidx)
                );
            }

            triangulos.AddRange(triangulosTapa);
             triangulos.AddRange(triangulosLado);
            var offaltura = Vector3.forward*altura*escala;

            verticesTotales = new List<Vector3>();
            verticesTotales.AddRange(verticesBase.Select(v=>v+offaltura));
            verticesTotales.AddRange(verticesBase);

            meshGenerada.vertices = verticesTotales.ToArray();
            meshGenerada.triangles = triangulos.ToArray();
            meshGenerada.RecalculateNormals();
            meshGenerada.RecalculateBounds();
            meshGenerada.RecalculateTangents();
        }
    }

    [ContextMenu("MeshificarMuros")]
    public void MeshificarMuros()
    {
        if (contornos != null && tirarMeshAca)
        {
            if (meshGenerada == null) meshGenerada = new Mesh();
            else
            {
                meshGenerada.Clear();
            }
            tirarMeshAca.sharedMesh = meshGenerada;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            var vertCountEnUnaCapa = contornos.Sum(cont => cont.vertices.Length + cont.contornosInternos.Sum(subc => subc.vertices.Length));

            foreach (var contorno in contornos)
            {

                {
                    var vertCont = contorno.vertices;
                    var baseVertIndexes = vertices.Count;
                    for (int i = 0; i < vertCont.Length; i++)
                    {
                        vertices.Add(vertCont[i] * escala);
                        //vertices.Add((Vector3)vertCont[i]*escala+Vector3.forward*altura*escala);
                        int i2 = (i + 1) % vertCont.Length;
                        triangles.Add(i + baseVertIndexes);//este
                        triangles.Add(i + vertCountEnUnaCapa + baseVertIndexes);//este arriba
                        triangles.Add(i2 + baseVertIndexes);//siguiente

                        triangles.Add(i + vertCountEnUnaCapa + baseVertIndexes);//este arriba
                        triangles.Add(i2 + vertCountEnUnaCapa + baseVertIndexes);//siguiente arriba
                        triangles.Add(i2 + baseVertIndexes);//siguiente
                    }
                }

                foreach (var continterno in contorno.contornosInternos)
                {
                    var vertCont = continterno.vertices;
                    var baseVertIndexes = vertices.Count;
                    for (int i = 0; i < vertCont.Length; i++)
                    {
                        vertices.Add(vertCont[i] * escala);
                        //vertices.Add((Vector3)vertCont[i]*escala+Vector3.forward*10*escala);
                        int i2 = (i + 1) % vertCont.Length;
                        triangles.Add(i + baseVertIndexes);//este
                        triangles.Add(i + vertCountEnUnaCapa + baseVertIndexes);//este arriba
                        triangles.Add(i2 + baseVertIndexes);//siguiente

                        triangles.Add(i + vertCountEnUnaCapa + baseVertIndexes);//este arriba
                        triangles.Add(i2 + vertCountEnUnaCapa + baseVertIndexes);//siguiente arriba
                        triangles.Add(i2 + baseVertIndexes);//siguiente
                    }
                }

            }
            var offsetAltura = Vector3.forward * altura * escala;
            meshGenerada.vertices = vertices.Concat(vertices.Select(v => v + offsetAltura)).ToArray();
            //meshGenerada.vertices = vertices.ToArray();
            meshGenerada.triangles = triangles.ToArray();
            meshGenerada.RecalculateNormals();
            meshGenerada.RecalculateBounds();
            meshGenerada.RecalculateTangents();

        }
    }

    [ContextMenu("MeshificarPlano")]
    public void MeshificarPlano()
    {
        if (contornos != null && tirarMeshAca)
        {
            if (meshGenerada == null) meshGenerada = new Mesh();
            else
            {
                meshGenerada.Clear();
            }
            tirarMeshAca.sharedMesh = meshGenerada;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            var idxOffset = 0;
            for (int i = 0, n = contornos.Count; i < n; i++)
            {
                var contorno = contornos[i];
                Tess teselador = new Tess();
                teselador.AddContour(contornos[i].vertices.Select((v,idx) => new ContourVertex(new Vec3(v.x, v.y, 0))).ToArray(), ContourOrientation.Clockwise);
                idxOffset += contorno.vertices.Length;
                foreach (var continterno in contorno.contornosInternos)
                {
                    teselador.AddContour(continterno.vertices.Select((v,idx) => new ContourVertex(new Vec3(v.x, v.y, 0))).ToArray(), ContourOrientation.CounterClockwise);
                    idxOffset += continterno.vertices.Length;
                }
                teselador.Tessellate(WindingRule.Positive, ElementType.Polygons, 3, VertexCombine);

                contorno.contornosInternos = new Contorno[0];
                
                contorno.vertices = teselador.Vertices
                    .Select(v => new Vector3(v.Position.X, v.Position.Y, 0) * escala).ToArray();

                triangles.AddRange(teselador.Elements.Select(e => e + vertices.Count));
                vertices.AddRange(contorno.vertices);

                for (int j = 0, n2 = contorno.vertices.Length; j < n2; j++) contorno.vertices[j] /= escala;
                //contornos[i] = contorno;
            }
            
            meshGenerada.vertices = vertices.ToArray();
            meshGenerada.triangles = triangles.ToArray();
            meshGenerada.RecalculateNormals();
            meshGenerada.RecalculateBounds();
            meshGenerada.RecalculateTangents();

        }
    }

    class ContVertIdx{
        public Contorno contorno;
        public int vidx;
        public ContVertIdx(Contorno contorno, int vidx) {
            this.contorno = contorno;
            this.vidx = vidx;
        }
    }
    private static object VertexCombine(LibTessDotNet.Vec3 position, object[] data, float[] weights)
    {
        var resultado = data[0];
        float wMayor = weights[0];
        for (int i=1,n=weights.Length; i<n; i++) {
            if (weights[i] > wMayor) {
                resultado = data[i];
                wMayor = weights[i];
            }
        }
        return resultado;
    }
}
