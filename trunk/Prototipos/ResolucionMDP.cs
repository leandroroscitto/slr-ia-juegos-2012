using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PruebasMarkov2 {
   public class ResolucionMDP {
	  public class TransicionJuego : Transicion_MDP<Arbol_Estados.Nodo_Estado, Accion> {
		 public override float valor(Accion a, Arbol_Estados.Nodo_Estado s, Arbol_Estados.Nodo_Estado sp) {
			if (s.estados_hijos != null) {
			   int indice = s.estados_hijos.IndexOf(sp);
			   if ((indice >= 0) && (s.acciones_hijos[indice] == a)) {
				  return (1f / s.acciones_hijos.Count);
				  //return 1f;
			   }
			   else
				  return 0f;
			}
			else {
			   // Nunca deberia llegar por aca.
			   return -1f;
			}
		 }
	  }

	  public class RecompensaJuego : Recompensa_MDP<Arbol_Estados.Nodo_Estado, Juego.Objetivo> {
		 Juego.Objetivo[] objetivos;

		 public RecompensaJuego(ref Juego.Objetivo[] objs)
			: base() {
			objetivos = objs;
		 }

		 public override float valor(Arbol_Estados.Nodo_Estado s, Juego.Objetivo o, int actor_id) {
			float resultado = (s.estado_actual.objetivos_cumplidos.Count - s.estado_actual.objetivos_no_cumplidos.Count);
			if (s.estado_actual.objetivos_no_cumplidos.Contains(o.id)) {
			   float distancia_minima = float.MaxValue;
			   for (int actor = 0; actor < s.estado_actual.posicion_jugadores.Count; actor++) {
				  if (actor == actor_id) {
					 resultado -= s.estado_actual.posicion_jugadores[actor].distancia(o.posicion);
				  }
				  else {
					 distancia_minima = Math.Min(distancia_minima, s.estado_actual.posicion_jugadores[actor].distancia(o.complementario.posicion));
					 //distancia_minima = 0;
				  }
			   }
			   if (s.estado_actual.posicion_jugadores.Count > 1)
				  resultado -= distancia_minima/2;
			}
			else {
			   //System.Diagnostics.Debug.Assert(false);
			}

			return resultado;
		 }
	  }

	  public Arbol_Estados arbol_estados;
	  public MDP<Arbol_Estados.Nodo_Estado, Accion, Juego.Objetivo, TransicionJuego, RecompensaJuego> mdp;

	  public ResolucionMDP(ref Arbol_Estados ae) {
		 arbol_estados = ae;
		 TransicionJuego transicion = new TransicionJuego();
		 RecompensaJuego recompensa = new RecompensaJuego(ref arbol_estados.objetivos);

		 Arbol_Estados.Nodo_Estado[] estados = arbol_estados.estados.ToArray();
		 Accion[] acciones = arbol_estados.acciones_individuales.ToArray();
		 mdp = new MDP<Arbol_Estados.Nodo_Estado, Accion, Juego.Objetivo, TransicionJuego, RecompensaJuego>(estados, acciones, arbol_estados.objetivos, arbol_estados.jugadores.Length, transicion, recompensa, 0.65f);
	  }
   }
}
