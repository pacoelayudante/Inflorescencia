using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ControlAgentes
{
    public ConfigAgentesDeFlor config;
    
    public Agente[] agentesVivos = new Agente[0];
    public int Length => agentesVivos.Length;
    public Agente this[int ind] => agentesVivos[ind];

    public float distCerca => config._distCerca;// = 30f;
    public float multRepulsion => config._multRepulsion;// = 4f;
    public float multAtraccion => config._multAtraccion;// 0.0001f;
    public float expansion => config._expansion;// 0.1f;
    public float escalaFuerza => config._escalaFuerza;// 0.01f;
    public float decaeFuerza => config._decaeFuerza;// 1f;

    public ControlAgentes(ConfigAgentesDeFlor config, List<Contorno> contornos, float escala, Vector3 offsetEscalado) {
        this. config = config;

        if (contornos == null || contornos.Count == 0) return;
        
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
                    var d = Vector3.Distance(v, otro) * escala;
                    if (d < distCerca)
                    {
                        cercanos.Add(new Agente.AgenteComboIndexDist(j + listaAgentes.Count, d));
                    }
                }
                agentes.Add(new Agente(v * escala + offsetEscalado, cercanos.ToArray()));
            }

            listaAgentes.AddRange(agentes);
        }

        agentesVivos = listaAgentes.ToArray();
    }

    public void UpdateAgentes(float dt = 0.01f) {
        UpdateRepulsion(dt);
        UpdateCercanos(dt);
        UpdateFuerzas(dt);
    }

    void UpdateCercanos(float dt = 0.01f)
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
                var xoff = otro.X - agente.X;
                var yoff = otro.Y - agente.Y;
                var sqDist = Vector2.Distance(agente.pos, otro.pos) - dBase;

                xoff *= multAtraccion * sqDist * dt;
                yoff *= multAtraccion * sqDist * dt;
                otro.FueX -= xoff;
                otro.FueY -= yoff;
                agente.FueX += xoff;
                agente.FueY += yoff;
                agente.cercanosDist[iOtro] += expansion * dt;
            }
        }
    }
    void UpdateRepulsion(float dt = 0.01f)
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
                var xoff = otro.X - agente.X;
                var yoff = otro.Y - agente.Y;
                var sqDist = xoff * xoff + yoff * yoff;

                if (sqDist == 0f) continue;//ignorar posibles puntos solapados exactos (en realidad deberia limiparlos de la lista)

                xoff *= multRepulsion * dt / sqDist;
                yoff *= multRepulsion * dt / sqDist;
                otro.FueX += xoff;
                otro.FueY += yoff;
                agente.FueX -= xoff;
                agente.FueY -= yoff;
            }
        }
    }
    void UpdateFuerzas(float dt = 0.01f)
    {
        for (int i = 0, n = agentesVivos.Length; i < n; i++)
        {
            agentesVivos[i].X += agentesVivos[i].FueX * dt * escalaFuerza;
            agentesVivos[i].FueX *= decaeFuerza;
            //verticesVivos[i].x = agentesVivos[i].x;

            agentesVivos[i].Y += agentesVivos[i].FueY * dt * escalaFuerza;
            agentesVivos[i].FueY *= decaeFuerza;
            //verticesVivos[i].y = agentesVivos[i].y;

            //verticesVivos[i].z += dt * velocidadAltura;
        }
    }

    public class Agente
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
            X = p.x;
            Y = p.y;
            if (cercanos != null) this.cercanosIdx = cercanos;
            if (cercanosD != null) this.cercanosDist = cercanosD;
        }

        public Agente(Vector2 p, AgenteComboIndexDist[] cercanos = null)
        {
            X = p.x;
            Y = p.y;
            if (cercanos != null)
            {
                this.cercanosIdx = cercanos.Select(c => c.idx).ToArray();
                this.cercanosDist = cercanos.Select(c => c.dist).ToArray();
            }
        }
        public Vector2 pos,fue;
        //public float x, y;
        public float X {
            get => pos.x;
            set => pos.x = value;
        }
        public float Y {
            get => pos.y;
            set => pos.y = value;
        }
        //public float fX, fY;
        public float FueX {
            get => fue.x;
            set => fue.x = value;
        }
        public float FueY {
            get => fue.y;
            set => fue.y = value;
        }
        public int[] cercanosIdx;
        public float[] cercanosDist;
    }
}
