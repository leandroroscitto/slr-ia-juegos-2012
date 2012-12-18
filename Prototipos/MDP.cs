using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PruebasMarkov2 {
   [Serializable]
   public abstract class Estado_MDP {
	  public int id;

	  public abstract Estado_MDP[] proximosEstados();
	  public abstract Accion_MDP[] accionesValidas();
	  public abstract Estado_MDP transicionAccion(Accion_MDP a);
   }
   [Serializable]
   public abstract class Accion_MDP {
	  public int id;
	  public int actor;
   }
   [Serializable]
   public abstract class Transicion_MDP<S, A> {
	  public abstract float valor(A a, S s, S sp);
   }
   [Serializable]
   public abstract class Recompensa_MDP<S> {
	  public abstract float valor(S s);
   }

   public class MDP<S, A, T, R>
	  where S : Estado_MDP
	  where A : Accion_MDP
	  where T : Transicion_MDP<S, A>
	  where R : Recompensa_MDP<S> {
	  public List<S> estados;
	  public List<A> acciones;
	  public int numero_actores;
	  public T transicion;
	  public R recompensa;
	  public float factor_descuento;

	  public float[][] Utilidad;
	  public A[][] Politica;

	  public MDP(S[] est, A[] acs, int na, T trn, R rep, float fac) {
		 estados = new List<S>(est);
		 for (int i = 0; i < estados.Count; i++)
			estados[i].id = i;
		 acciones = new List<A>(acs);
		 for (int j = 0; j < acciones.Count; j++)
			acciones[j].id = j;
		 numero_actores = na;
		 transicion = trn;
		 recompensa = rep;
		 factor_descuento = fac;

		 //Calcular_Utilidad_VI();
		 Calcular_Utilidad_PI();
	  }

	  public void Calcular_Utilidad_PI() {
		 float[][] Utilidad_Aux = new float[numero_actores][];
		 A[][] Politica_Aux = new A[numero_actores][];
		 float[][] Value_Policy = new float[numero_actores][];
		 for (int actor = 0; actor < numero_actores; actor++) {
			Utilidad_Aux[actor] = new float[estados.Count];
			Politica_Aux[actor] = new A[estados.Count];
			Value_Policy[actor] = new float[estados.Count];
		 }

		 Random R = new Random();
		 foreach (S i in estados) {
			A[] acciones_validas = (A[])i.accionesValidas();

			for (int actor = 0; actor < numero_actores; actor++) {
			   int accion_index = 0;
			   A accion = null;
			   do {
				  accion = acciones_validas[accion_index];
				  accion_index++;
			   } while (accion.actor != actor);
			   Politica_Aux[actor][i.id] = accion;
			   Utilidad_Aux[actor][i.id] = recompensa.valor(i);
			}
		 }


		 bool sincambios;
		 do {
			// Value_Determination
			for (int actor = 0; actor < numero_actores; actor++) {
			   foreach (S i in estados) {
				  Utilidad_Aux[actor][i.id] = recompensa.valor(i) + Value_Policy[actor][i.id];
			   }
			}
			sincambios = true;

			foreach (S i in estados) {
			   A[] action_max = new A[numero_actores];
			   float[] value_max = new float[numero_actores];
			   for (int actor = 0; actor < numero_actores; actor++) {
				  action_max[actor] = null;
				  value_max[actor] = float.MinValue;
			   }

			   foreach (A a in i.accionesValidas()) {
				  float value = 0;
				  foreach (S j in i.proximosEstados()) {
					 value += transicion.valor(a, i, j) * Utilidad_Aux[a.actor][j.id];
				  }
				  if (value > value_max[a.actor]) {
					 value_max[a.actor] = value;
					 action_max[a.actor] = a;
				  }
			   }

			   float[] value_policy = new float[numero_actores];
			   for (int actor = 0; actor < numero_actores; actor++) {
				  foreach (S j in i.proximosEstados()) {
					 value_policy[actor] += transicion.valor(Politica_Aux[actor][i.id], i, j) * Utilidad_Aux[actor][j.id];
				  }

				  if (value_max[actor] > value_policy[actor]) {
					 Politica_Aux[actor][i.id] = action_max[actor];
					 sincambios = false;
					 Value_Policy[actor][i.id] = value_max[actor];
				  }
				  else {
					 Value_Policy[actor][i.id] = value_policy[actor];
				  }
			   }

			   Console.WriteLine("Progreso: " + (i.id * 100f / estados.Count));
			}
		 } while (!sincambios);

		 Utilidad = Utilidad_Aux;
		 Politica = Politica_Aux;
	  }


	  public void Calcular_Utilidad_VI() {
		 float[][] Utilidad_Aux = new float[numero_actores][];
		 A[][] Politica_Aux = new A[numero_actores][];
		 float[][] Utilidad = new float[numero_actores][];
		 for (int actor = 0; actor < numero_actores; actor++) {
			Utilidad_Aux[actor] = new float[estados.Count];
			Politica_Aux[actor] = new A[estados.Count];
			Utilidad[actor] = new float[estados.Count];
		 }

		 for (int actor = 0; actor < numero_actores; actor++) {
			foreach (S i in estados) {
			   Utilidad_Aux[actor][i.id] = recompensa.valor(i);
			}
		 }

		 do {
			Array.Copy(Utilidad_Aux, Utilidad, Utilidad_Aux.Length);

			int count = 0;
			foreach (S i in estados) {
			   float[] value_max = new float[numero_actores];
			   for (int actor = 0; actor < numero_actores; actor++) {
				  value_max[actor] = float.MinValue;
			   }
			   foreach (A a in i.accionesValidas()) {
				  float value = 0;
				  foreach (S j in i.proximosEstados()) {
					 value += transicion.valor(a, i, j) * Utilidad_Aux[a.actor][j.id];
				  }
				  if (value > value_max[a.actor]) {
					 value_max[a.actor] = value;
					 Politica_Aux[a.actor][i.id] = a;
				  }
				  count++;
				  Console.WriteLine("Completado: " + (count * 1f / (estados.Count * acciones.Count)));
			   }

			   for (int actor = 0; actor < numero_actores; actor++) {
				  Utilidad_Aux[actor][i.id] = recompensa.valor(i) + value_max[actor];
			   }
			}

		 } while (!similares(Utilidad_Aux, Utilidad, 0.2f));

		 Utilidad = Utilidad_Aux;
		 Politica = Politica_Aux;
	  }

	  public bool similares(float[][] a, float[][] b, float delta) {
		 double suma = 0;
		 for (int actor = 0; actor < numero_actores; actor++) {
			foreach (S i in estados) {
			   suma += Math.Pow(a[actor][i.id] - b[actor][i.id], 2);
			}
		 }

		 double rms = Math.Sqrt(suma) / estados.Count;
		 Console.WriteLine(rms);
		 return (rms < delta);
	  }
   }
}
