﻿using System;
using System.Collections.Generic;
using libtcod;

namespace PruebasMarkov2 {
   public class Arbol_Estados {
	  public class Nodo_Estado : Estado_MDP {
		 public Estado estado_actual;
		 public List<Nodo_Estado> estados_padres;
		 // <id_jugador, estados>
		 public Dictionary<int, List<Nodo_Estado>> estados_padres_actor;
		 public List<Nodo_Estado> estados_hijos;
		 // <id_jugador, estados>
		 public Dictionary<int, List<Nodo_Estado>> estados_hijos_actor;
		 public List<Accion> acciones_padres;
		 // <id_jugador, acciones>
		 public Dictionary<int, List<Accion>> acciones_padres_actor;
		 public List<Accion> acciones_hijos;
		 // <id_jugador, acciones>
		 public Dictionary<int, List<Accion>> acciones_hijos_actor;

		 public Nodo_Estado(Estado e) {
			estado_actual = e;
			estados_padres = new List<Nodo_Estado>();
			estados_padres_actor = new Dictionary<int, List<Nodo_Estado>>();
			estados_hijos = new List<Nodo_Estado>();
			estados_hijos_actor = new Dictionary<int, List<Nodo_Estado>>();
			acciones_padres = new List<Accion>();
			acciones_padres_actor = new Dictionary<int, List<Accion>>();
			acciones_hijos = new List<Accion>();
			acciones_hijos_actor = new Dictionary<int, List<Accion>>();

			foreach (int jugador_id in e.posicion_jugadores.Keys) {
			   acciones_padres_actor.Add(jugador_id, new List<Accion>());
			   acciones_hijos_actor.Add(jugador_id, new List<Accion>());
			   estados_padres_actor.Add(jugador_id, new List<Nodo_Estado>());
			   estados_hijos_actor.Add(jugador_id, new List<Nodo_Estado>());
			}
		 }

		 public int AgregarHijo(Nodo_Estado h, Accion ja) {
			estados_hijos.Add(h);
			estados_hijos_actor[ja.actor_id].Add(h);
			acciones_hijos.Add(ja);
			acciones_hijos_actor[ja.actor_id].Add(ja);

			h.AgregarPadre(this, ja);
			return estados_hijos.Count - 1;
		 }

		 public int AgregarPadre(Nodo_Estado p, Accion ja) {
			estados_padres.Add(p);
			estados_padres_actor[ja.actor_id].Add(p);
			acciones_padres.Add(ja);
			acciones_padres_actor[ja.actor_id].Add(ja);

			return estados_padres.Count - 1;
		 }

		 public override Estado_MDP hijoAccion(Accion_MDP a) {
			int index = acciones_hijos.IndexOf((Accion)a);
			if (index >= 0)
			   return estados_hijos[index];
			else
			   return null;
		 }

		 public override Estado_MDP padreAccion(Accion_MDP a) {
			int index = acciones_padres.IndexOf((Accion)a);
			if (index >= 0)
			   return estados_padres[index];
			else
			   return null;
		 }

		 public override Accion_MDP[] accionesValidas(int actor_id) {
			if (actor_id == -1) {
			   return acciones_hijos.ToArray();
			}
			else {
			   return acciones_hijos_actor[actor_id].ToArray();
			}
		 }

		 public override Estado_MDP[] proximosEstados(int actor_id) {
			if (actor_id == -1) {
			   return estados_hijos.ToArray();
			}
			else {
			   return estados_hijos_actor[actor_id].ToArray();
			}
		 }

		 public override string ToString() {
			return "Estado_ID: " + estado_actual.id + ", hijos: " + estados_hijos.Count + ", padres: " + estados_padres.Count;
		 }
	  }

	  public Juego.Zona[][] escenario_base;
	  public Juego.Objetivo[] objetivos;
	  public Juego.Jugador[] jugadores;
	  public List<Accion> acciones_individuales;
	  // <numero_objetivos_cumplidos, <posicion_player_0, nodo_estado>>
	  public Dictionary<int, Dictionary<Vector2[], List<Nodo_Estado>>> estados_dict;
	  public List<Nodo_Estado> estados;
	  public Nodo_Estado nodo_estado_inicial;
	  public Dictionary<int, Dictionary<Vector2[], List<Nodo_Estado>>> frontera_dict;
	  private Queue<Nodo_Estado> frontera;

	  private Nodo_Estado nodo_estado_actual;
	  private int cant_estados;

	  public Arbol_Estados(ref Juego.Zona[][] eb, ref Juego.Jugador[] jugs, ref List<Accion> accs, ref Juego.Objetivo[] objs) {
		 escenario_base = eb;
		 acciones_individuales = accs;
		 objetivos = objs;
		 jugadores = jugs;

		 PrepararEstados();
	  }

	  private void PrepararEstados() {
		 estados_dict = new Dictionary<int, Dictionary<Vector2[], List<Nodo_Estado>>>();
		 estados = new List<Nodo_Estado>();
		 frontera_dict = new Dictionary<int, Dictionary<Vector2[], List<Nodo_Estado>>>();
		 frontera = new Queue<Nodo_Estado>();

		 cant_estados = 0;
		 Estado estado_inicial = new Estado(cant_estados, ref escenario_base);
		 foreach (Juego.Objetivo objetivo in objetivos) {
			estado_inicial.objetivos_no_cumplidos.Add(objetivo.id);
		 }
		 foreach (Juego.Jugador jugador in jugadores) {
			estado_inicial.posicion_jugadores.Add(jugador.id, jugador.posicion);
		 }
		 VerificarCumplimientoObjetivos(estado_inicial);

		 nodo_estado_inicial = new Nodo_Estado(estado_inicial);
		 frontera.Enqueue(nodo_estado_inicial);
		 AgregarEstadoDict(nodo_estado_inicial, frontera_dict);

		 while (frontera.Count > 0) {
			nodo_estado_actual = frontera.Dequeue();
			RemoverEstadoDict(nodo_estado_actual, frontera_dict);

			Debuging();

			foreach (Accion jugador_accion in acciones_individuales) {
			   foreach (Juego.Jugador jugador in jugadores) {
				  jugador.posicion = nodo_estado_actual.estado_actual.posicion_jugadores[jugador.id];
			   }

			   int ja_jugador_id = jugador_accion.jugador.id;
			   int ja_accion_id = jugador_accion.id;
			   Vector2 nueva_posicion;
			   if (nodo_estado_actual.estado_actual.IntentarAccion(jugadores[ja_jugador_id], acciones_individuales[ja_accion_id], out nueva_posicion)) {
				  // Obtener el proximo estado a partir del actual y la accion del jugador.
				  bool en_visitado = false;
				  bool en_frontera = false;
				  Nodo_Estado proximo_estado_nodo = BuscarProximoEstado(nodo_estado_actual, jugadores[ja_jugador_id], nueva_posicion, out en_visitado, out en_frontera);
				  // Si no existe el proximo estado en niguna lista, lo crea.
				  if (proximo_estado_nodo == null) {
					 cant_estados++;
					 Estado proximo_estado = new Estado(cant_estados, ref escenario_base);
					 foreach (int objetivo_id in nodo_estado_actual.estado_actual.objetivos_cumplidos) {
						proximo_estado.objetivos_cumplidos.Add(objetivo_id);
					 }
					 foreach (int objetivo_id in nodo_estado_actual.estado_actual.objetivos_no_cumplidos) {
						proximo_estado.objetivos_no_cumplidos.Add(objetivo_id);
					 }
					 foreach (Juego.Jugador jugador in jugadores) {
						proximo_estado.posicion_jugadores.Add(jugador.id, jugador.posicion);
					 }
					 proximo_estado.posicion_jugadores[ja_jugador_id] = nueva_posicion;
					 VerificarCumplimientoObjetivos(proximo_estado);

					 proximo_estado_nodo = new Nodo_Estado(proximo_estado);
				  }

				  // Verificar si pertenezca a 'estados' o es igual que el estado actual.
				  // Si pertence, no hacer nada, la relacion padre-hijo ya tendria que haber sido establecida.
				  if (!en_visitado) {
					 // Si no pertenece, establecer la relacion padre-hijo.
					 nodo_estado_actual.AgregarHijo(proximo_estado_nodo, jugador_accion);

					 // Verifica si ya se encuentra en la frontera. De no ser asi lo agrega.
					 if (!en_frontera) {
						frontera.Enqueue(proximo_estado_nodo);
						AgregarEstadoDict(proximo_estado_nodo, frontera_dict);
					 }
				  }
				  else {
					 nodo_estado_actual.AgregarHijo(proximo_estado_nodo, jugador_accion);
				  }
			   }
			}

			AgregarEstadoDict(nodo_estado_actual, estados_dict);
			estados.Add(nodo_estado_actual);
		 }

		 foreach (Juego.Jugador jugador in jugadores) {
			jugador.posicion = estado_inicial.posicion_jugadores[jugador.id];
		 }
	  }

	  public class Comparador_Arreglo_Vector2 : IEqualityComparer<Vector2[]> {

		 public bool Equals(Vector2[] x, Vector2[] y) {
			if (x.Length == y.Length) {
			   for (int i = 0; i < x.Length; i++) {
				  if (!x[i].Equals(y[i]))
					 return false;
			   }
			   return true;
			}
			else
			   return false;
		 }

		 public int GetHashCode(Vector2[] obj) {
			int dimensiones = Juego.alto * Juego.ancho;
			int valor = 0;
			for (int i = 0; i < obj.Length; i++) {
			   valor += obj[i].GetHashCode() * (int)Math.Pow(Math.Floor(Math.Log10(dimensiones)), i);
			}
			return valor;
		 }
	  }

	  public static Comparador_Arreglo_Vector2 comparador = new Comparador_Arreglo_Vector2();

	  private void AgregarEstadoDict(Nodo_Estado estado, Dictionary<int, Dictionary<Vector2[], List<Nodo_Estado>>> dict) {
		 int cant_obj_cumplidos = estado.estado_actual.objetivos_cumplidos.Count;
		 Vector2[] posicion_jugadores = new Vector2[estado.estado_actual.posicion_jugadores.Count];
		 estado.estado_actual.posicion_jugadores.Values.CopyTo(posicion_jugadores, 0);

		 Dictionary<Vector2[], List<Nodo_Estado>> PosJugador_Estados;
		 if (dict.TryGetValue(cant_obj_cumplidos, out PosJugador_Estados)) {
			List<Nodo_Estado> Lista_Estados;
			if (PosJugador_Estados.TryGetValue(posicion_jugadores, out Lista_Estados)) {
			   Lista_Estados.Add(estado);
			}
			else {
			   Lista_Estados = new List<Nodo_Estado>();
			   Lista_Estados.Add(estado);
			   PosJugador_Estados.Add(posicion_jugadores, Lista_Estados);
			}
		 }
		 else {
			PosJugador_Estados = new Dictionary<Vector2[], List<Nodo_Estado>>(comparador);
			List<Nodo_Estado> Lista_Estados = new List<Nodo_Estado>();
			Lista_Estados.Add(estado);
			PosJugador_Estados.Add(posicion_jugadores, Lista_Estados);
			dict.Add(cant_obj_cumplidos, PosJugador_Estados);
		 }
	  }

	  private bool GetEstadoDict(Nodo_Estado estado, Dictionary<int, Dictionary<Vector2[], List<Nodo_Estado>>> dict, out Nodo_Estado nodo_estado_resultado) {
		 int cant_obj_cumplidos = estado.estado_actual.objetivos_cumplidos.Count;
		 Vector2[] posicion_jugadores = new Vector2[estado.estado_actual.posicion_jugadores.Count];
		 estado.estado_actual.posicion_jugadores.Values.CopyTo(posicion_jugadores, 0);

		 Dictionary<Vector2[], List<Nodo_Estado>> PosJugador_Estados;
		 if (dict.TryGetValue(cant_obj_cumplidos, out PosJugador_Estados)) {
			List<Nodo_Estado> Lista_Estados;
			if (PosJugador_Estados.TryGetValue(posicion_jugadores, out Lista_Estados)) {
			   foreach (Nodo_Estado nodo_estado in Lista_Estados) {
				  if (nodo_estado.estado_actual.Equals(estado.estado_actual)) {
					 nodo_estado_resultado = nodo_estado;
					 return true;
				  }
			   }
			}
		 }

		 nodo_estado_resultado = null;
		 return false;
	  }

	  private void RemoverEstadoDict(Nodo_Estado estado, Dictionary<int, Dictionary<Vector2[], List<Nodo_Estado>>> dict) {
		 int cant_obj_cumplidos = estado.estado_actual.objetivos_cumplidos.Count;
		 Vector2[] posicion_jugadores = new Vector2[estado.estado_actual.posicion_jugadores.Count];
		 estado.estado_actual.posicion_jugadores.Values.CopyTo(posicion_jugadores, 0);

		 Dictionary<Vector2[], List<Nodo_Estado>> PosJugador_Estados;
		 if (dict.TryGetValue(cant_obj_cumplidos, out PosJugador_Estados)) {
			List<Nodo_Estado> Lista_Estados;
			if (PosJugador_Estados.TryGetValue(posicion_jugadores, out Lista_Estados)) {
			   Lista_Estados.Remove(estado);
			}
		 }
	  }

	  private void Debuging() {
		 if (cant_estados % 100 == 0) {
			Console.WriteLine("Estados procesados: " + cant_estados + ", y en frontera: " + frontera.Count);
			/*
			TCODConsole.root.setBackgroundColor(TCODColor.sepia);
			TCODConsole.root.clear();
			ImprimirEscenario(10, 10);
			ImprimirJugadores(10, 10);
			TCODConsole.flush();
			 */
			//TCODConsole.waitForKeypress(true);
		 }
	  }

	  public void ModificarEstado(Estado estado, Juego.Jugador jugador, Vector2 nueva_posicion, out Vector2 antigua_posicion, out List<int> objetivos_modificados) {
		 antigua_posicion = estado.posicion_jugadores[jugador.id];
		 estado.posicion_jugadores[jugador.id] = nueva_posicion;
		 objetivos_modificados = VerificarCumplimientoObjetivos(estado);
	  }

	  public void RestaurarEstado(Estado estado, Juego.Jugador jugador, Vector2 antigua_posicion, List<int> objetivos_modificados) {
		 estado.posicion_jugadores[jugador.id] = antigua_posicion;
		 foreach (int objetivo_id in objetivos_modificados) {
			estado.objetivos_cumplidos.Remove(objetivo_id);
			estado.objetivos_no_cumplidos.Add(objetivo_id);
		 }
	  }

	  public Nodo_Estado BuscarProximoEstado(Nodo_Estado estado, Juego.Jugador jugador, Vector2 nueva_posicion, out bool en_visitado, out bool en_frontera) {
		 Vector2 posicion_actual;
		 List<int> objetivos_modificados;
		 ModificarEstado(estado.estado_actual, jugador, nueva_posicion, out posicion_actual, out objetivos_modificados);
		 if (!nueva_posicion.Equals(posicion_actual)) {
			Nodo_Estado nodo_estado;
			if (GetEstadoDict(estado, estados_dict, out nodo_estado)) {
			   RestaurarEstado(estado.estado_actual, jugador, posicion_actual, objetivos_modificados);
			   en_visitado = true;
			   en_frontera = false;
			   return nodo_estado;
			}

			if (GetEstadoDict(estado, frontera_dict, out nodo_estado)) {
			   RestaurarEstado(estado.estado_actual, jugador, posicion_actual, objetivos_modificados);
			   en_visitado = false;
			   en_frontera = true;
			   return nodo_estado;
			}

			RestaurarEstado(estado.estado_actual, jugador, posicion_actual, objetivos_modificados);
			en_visitado = false;
			en_frontera = false;
			return null;
		 }
		 else {
			RestaurarEstado(estado.estado_actual, jugador, posicion_actual, objetivos_modificados);
			en_visitado = false;
			en_frontera = true;
			return nodo_estado_actual;
		 }
	  }

	  public List<int> VerificarCumplimientoObjetivos(Estado estado) {
		 bool[] objetivos_cumplidos = new bool[objetivos.Length];
		 foreach (Vector2 posicion_jugador in estado.posicion_jugadores.Values) {
			Juego.Zona zona = escenario_base[posicion_jugador.x][posicion_jugador.y];
			if (zona.tipo == Juego.Zona.TZona.OBJETIVO) {
			   Juego.Objetivo objetivo = (Juego.Objetivo)zona;
			   objetivos_cumplidos[objetivo.id] = true;
			}
		 }

		 List<int> nuevos_cumplidos = new List<int>();
		 foreach (int objetivo_id in estado.objetivos_no_cumplidos) {
			Juego.Objetivo objetivo = objetivos[objetivo_id];
			if (objetivos_cumplidos[objetivo.id] && objetivos_cumplidos[objetivo.complementario.id]) {
			   nuevos_cumplidos.Add(objetivo.id);
			   nuevos_cumplidos.Add(objetivo.complementario.id);
			}
		 }

		 foreach (int objetivo_id in nuevos_cumplidos) {
			estado.objetivos_no_cumplidos.Remove(objetivo_id);
			estado.objetivos_cumplidos.Add(objetivo_id);
		 }

		 return nuevos_cumplidos;
	  }
   }
}
