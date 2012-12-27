using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PruebasMarkov2 {
   public abstract class Estado_MDP {
	  public int id;

	  public abstract Estado_MDP[] proximosEstados();
	  public abstract Accion_MDP[] accionesValidas(int actor_id);
	  public abstract Estado_MDP hijoAccion(Accion_MDP a);
	  public abstract Estado_MDP padreAccion(Accion_MDP a);
   }
   public abstract class Accion_MDP {
	  public int id;
	  public int actor_id;
   }
   public interface Objetivo_MDP {
	  int GetID();
   }
   public abstract class Transicion_MDP<S, A> {
	  public abstract float valor(A a, S s, S sp);
   }
   public abstract class Recompensa_MDP<S, O> {
	  public abstract float valor(S s, O o, int actor_id);
   }

   public class MDP<S, A, O, T, R>
	  where S : Estado_MDP
	  where A : Accion_MDP
	  where O : Objetivo_MDP
	  where T : Transicion_MDP<S, A>
	  where R : Recompensa_MDP<S, O> {
	  public List<S> estados;
	  public List<A> acciones;
	  public List<O> objetivos;
	  public int numero_actores;
	  public T transicion;
	  public R recompensa;
	  public float factor_descuento;

	  // <jugador_id, objetivo_id, estado_id>
	  public float[][][] Utilidad;
	  public A[][][] Politica;

	  public MDP(S[] est, A[] acs, O[] objs, int na, T trn, R rep, float fac) {
		 estados = new List<S>(est);
		 for (int i = 0; i < estados.Count; i++)
			estados[i].id = i;
		 acciones = new List<A>(acs);
		 for (int j = 0; j < acciones.Count; j++)
			acciones[j].id = j;
		 objetivos = new List<O>();
		 for (int q = 0; q < objs.Length; q++) {
			objetivos.Insert(objs[q].GetID(), objs[q]);
		 }

		 numero_actores = na;
		 transicion = trn;
		 recompensa = rep;
		 factor_descuento = fac;

		 //Calcular_Utilidad_VI();
		 Calcular_Utilidad_PI();
	  }

	  public void Calcular_Utilidad_PI() {
		 float[][][] Utilidad_Aux = new float[numero_actores][][];
		 A[][][] Politica_Aux = new A[numero_actores][][];
		 float[][][] Value_Policy = new float[numero_actores][][];
		 for (int actor = 0; actor < numero_actores; actor++) {
			Utilidad_Aux[actor] = new float[objetivos.Count][];
			Politica_Aux[actor] = new A[objetivos.Count][];
			Value_Policy[actor] = new float[objetivos.Count][];
			for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
			   Utilidad_Aux[actor][objetivo_id] = new float[estados.Count];
			   Politica_Aux[actor][objetivo_id] = new A[estados.Count];
			   Value_Policy[actor][objetivo_id] = new float[estados.Count];
			   foreach (S i in estados) {
				  Utilidad_Aux[actor][objetivo_id][i.id] = 0;
				  Politica_Aux[actor][objetivo_id][i.id] = null;
				  Value_Policy[actor][objetivo_id][i.id] = 0;
			   }
			}
		 }

		 Random R = new Random();
		 foreach (S i in estados) {
			for (int actor = 0; actor < numero_actores; actor++) {
			   A[] acciones_validas = (A[])i.accionesValidas(actor);
			   for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
				  A accion = acciones_validas[R.Next(0, acciones_validas.Length)];
				  Politica_Aux[actor][objetivo_id][i.id] = accion;
				  Utilidad_Aux[actor][objetivo_id][i.id] = recompensa.valor(i, objetivos[objetivo_id], actor);
			   }
			}
		 }

		 bool sincambios;
		 float diferencia_total;
		 do {
			// Value_Determination
			foreach (S i in estados) {
			   for (int actor = 0; actor < numero_actores; actor++) {
				  for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
					 Utilidad_Aux[actor][objetivo_id][i.id] = recompensa.valor(i, objetivos[objetivo_id], actor) + factor_descuento * Value_Policy[actor][objetivo_id][i.id];
				  }
			   }
			}
			sincambios = true;
			diferencia_total = 0;

			foreach (S i in estados) {
			   A[][] action_max = new A[numero_actores][];
			   float[][] value_max = new float[numero_actores][];
			   for (int actor = 0; actor < numero_actores; actor++) {
				  action_max[actor] = new A[objetivos.Count];
				  value_max[actor] = new float[objetivos.Count];
				  for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
					 action_max[actor][objetivo_id] = null;
					 value_max[actor][objetivo_id] = float.MinValue;
				  }
			   }

			   for (int actor = 0; actor < numero_actores; actor++) {
				  for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
					 foreach (A a in i.accionesValidas(actor)) {
						float value = 0;
						foreach (S j in i.proximosEstados()) {
						   // Estaba calculando la utilidad para solo un jugador de la accion:
						   value += transicion.valor(a, i, j) * Utilidad_Aux[actor][objetivo_id][j.id];
						}
						if (value > value_max[actor][objetivo_id]) {
						   value_max[actor][objetivo_id] = value;
						   action_max[actor][objetivo_id] = a;
						}
					 }
				  }
			   }


			   float[][] value_policy = new float[numero_actores][];
			   for (int actor = 0; actor < numero_actores; actor++) {
				  value_policy[actor] = new float[objetivos.Count];
				  for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
					 value_policy[actor][objetivo_id] = 0;
				  }
			   }

			   for (int actor = 0; actor < numero_actores; actor++) {
				  for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
					 foreach (S j in i.proximosEstados()) {
						value_policy[actor][objetivo_id] += transicion.valor(Politica_Aux[actor][objetivo_id][i.id], i, j) * Utilidad_Aux[actor][objetivo_id][j.id];
					 }

					 float diferencia = Math.Abs(value_max[actor][objetivo_id] - value_policy[actor][objetivo_id]);
					 if (diferencia != 0) {
						diferencia_total += diferencia;
						Politica_Aux[actor][objetivo_id][i.id] = action_max[actor][objetivo_id];
						Value_Policy[actor][objetivo_id][i.id] = value_max[actor][objetivo_id];
						sincambios = false;
					 }
					 else {
						Value_Policy[actor][objetivo_id][i.id] = value_policy[actor][objetivo_id];
					 }
				  }
			   }

			   float porcentaje = (i.id * 100f / estados.Count);
			   if (porcentaje % 10 == 0)
				  Console.WriteLine("Progreso: " + (int)porcentaje + ", diferencia: " + diferencia_total);
			}
		 } while (!sincambios && diferencia_total > 0.0002f);

		 Utilidad = Utilidad_Aux;
		 Politica = Politica_Aux;
	  }


	  public void Calcular_Utilidad_VI() {
		 float[][][] Utilidad_Aux = new float[numero_actores][][];
		 A[][][] Politica_Aux = new A[numero_actores][][];
		 float[][][] Utilidad = new float[numero_actores][][];
		 for (int actor = 0; actor < numero_actores; actor++) {
			Utilidad_Aux[actor] = new float[objetivos.Count][];
			Politica_Aux[actor] = new A[objetivos.Count][];
			Utilidad[actor] = new float[objetivos.Count][];
			for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
			   Utilidad_Aux[actor][objetivo_id] = new float[estados.Count];
			   Politica_Aux[actor][objetivo_id] = new A[estados.Count];
			   Utilidad[actor][objetivo_id] = new float[estados.Count];
			}
		 }

		 for (int actor = 0; actor < numero_actores; actor++) {
			for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
			   foreach (S i in estados) {
				  Utilidad_Aux[actor][objetivo_id][i.id] = recompensa.valor(i, objetivos[objetivo_id], actor);
			   }
			}
		 }

		 do {
			for (int actor = 0; actor < Utilidad_Aux.Length; actor++) {
			   for (int objetivo = 0; objetivo < Utilidad_Aux[actor].Length; objetivo++) {
				  for (int estado = 0; estado < Utilidad_Aux[actor][objetivo].Length; estado++) {
					 Utilidad[actor][objetivo][estado] = Utilidad_Aux[actor][objetivo][estado];
				  }
			   }
			}

			int count = 0;
			foreach (S i in estados) {
			   float[][] value_max = new float[numero_actores][];
			   for (int actor = 0; actor < numero_actores; actor++) {
				  value_max[actor] = new float[objetivos.Count];
				  for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
					 value_max[actor][objetivo_id] = float.MinValue;
				  }
			   }

			   for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
				  foreach (A a in i.accionesValidas(-1)) {
					 float value = 0;
					 foreach (S j in i.proximosEstados()) {
						value += transicion.valor(a, i, j) * Utilidad_Aux[a.actor_id][objetivo_id][j.id];
					 }
					 if (value > value_max[a.actor_id][objetivo_id]) {
						value_max[a.actor_id][objetivo_id] = value;
						Politica_Aux[a.actor_id][objetivo_id][i.id] = a;
					 }
					 count++;
					 float porcentaje = (100 * (count * 1f / (estados.Count * acciones.Count * numero_actores)));
					 if (porcentaje % 10 == 0)
						Console.WriteLine("Completado: " + porcentaje);
				  }

				  for (int actor = 0; actor < numero_actores; actor++) {
					 Utilidad_Aux[actor][objetivo_id][i.id] = recompensa.valor(i, objetivos[objetivo_id], actor) + value_max[actor][objetivo_id];
				  }
			   }
			}

		 } while (!similares(Utilidad_Aux, Utilidad, 0.2f));

		 Utilidad = Utilidad_Aux;
		 Politica = Politica_Aux;
	  }

	  public bool similares(float[][][] a, float[][][] b, float delta) {
		 double suma = 0;
		 for (int actor = 0; actor < numero_actores; actor++) {
			for (int objetivo_id = 0; objetivo_id < objetivos.Count; objetivo_id++) {
			   foreach (S i in estados) {
				  suma += Math.Pow(a[actor][objetivo_id][i.id] - b[actor][objetivo_id][i.id], 2);
			   }
			}
		 }

		 double rms = Math.Sqrt(suma) / estados.Count;
		 Console.WriteLine(rms);
		 return (rms < delta);
	  }
   }
}
