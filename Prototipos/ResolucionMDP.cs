using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PruebasMarkov2 {
   public class ResolucionMDP {
	  public class TransicionJuego : Transicion_MDP<Arbol_Estados.Nodo_Estado, Accion> {
		 public override float valor(Accion a, Arbol_Estados.Nodo_Estado s, Arbol_Estados.Nodo_Estado sp) {
			if (sp.estados_padres != null) {
			   int indice = sp.estados_padres.IndexOf(s);
			   if ((indice >= 0) && (sp.acciones_padres[indice] == a))
				  return 1f;
			   else
				  return 0;
			}
			else {
			   // Nunca deberia llegar por aca.
			   return -1;
			}
		 }
	  }

	  public class RecompensaJuego : Recompensa_MDP<Arbol_Estados.Nodo_Estado> {
		 Juego.Objetivo[] objetivos;

		 public RecompensaJuego(ref Juego.Objetivo[] objs)
			: base() {
			objetivos = objs;
		 }

		 public override float valor(Arbol_Estados.Nodo_Estado s) {
			float resultado = (s.estado_actual.objetivos_cumplidos.Count - s.estado_actual.objetivos_no_cumplidos.Count);
			foreach (Juego.Objetivo objetivo in objetivos) {
			   if (s.estado_actual.objetivos_no_cumplidos.Contains(objetivo.id)) {
				  float distancia = float.MaxValue;
				  foreach (Vector2 posicion_jugador in s.estado_actual.posicion_jugadores.Values) {
					 distancia = Math.Min(distancia, posicion_jugador.distancia_directa(objetivo.posicion));
				  }
				  resultado -= distancia;
			   }
			}

			return resultado;
		 }
	  }

	  public Arbol_Estados arbol_estados;
	  public MDP<Arbol_Estados.Nodo_Estado, Accion, TransicionJuego, RecompensaJuego> mdp;

	  public ResolucionMDP(ref Arbol_Estados ae) {
		 arbol_estados = ae;
		 TransicionJuego transicion = new TransicionJuego();
		 RecompensaJuego recompensa = new RecompensaJuego(ref arbol_estados.objetivos);

		 Arbol_Estados.Nodo_Estado[] estados = arbol_estados.estados.ToArray();
		 Accion[] acciones = arbol_estados.acciones_individuales.ToArray();
		 mdp = new MDP<Arbol_Estados.Nodo_Estado, Accion, TransicionJuego, RecompensaJuego>(estados, acciones, arbol_estados.jugadores.Length, transicion, recompensa, 0.65f);
	  }
   }
}
