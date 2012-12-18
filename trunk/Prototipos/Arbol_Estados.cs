using System;
using System.Collections.Generic;
using libtcod;

namespace PruebasMarkov2 {
   public class Arbol_Estados {
	  public class Nodo_Estado : Estado_MDP {
		 public Estado estado_actual;
		 public List<Nodo_Estado> estados_padres;
		 public List<Nodo_Estado> estados_hijos;
		 // <id_jugador,id_accion>
		 public List<Accion> acciones_padres;
		 // <id_jugador,id_accion>
		 public List<Accion> acciones_hijos;

		 public Nodo_Estado(Estado e) {
			estado_actual = e;
			estados_padres = new List<Nodo_Estado>();
			estados_hijos = new List<Nodo_Estado>();
			acciones_padres = new List<Accion>();
			acciones_hijos = new List<Accion>();
		 }

		 public int AgregarHijo(Nodo_Estado h, Accion ja) {
			estados_hijos.Add(h);
			acciones_hijos.Add(ja);

			h.AgregarPadre(this, ja);
			return estados_hijos.Count - 1;
		 }

		 public int AgregarPadre(Nodo_Estado p, Accion ja) {
			estados_padres.Add(p);
			acciones_padres.Add(ja);
			return estados_padres.Count - 1;
		 }

		 public override Estado_MDP transicionAccion(Accion_MDP a) {
			int index = acciones_hijos.IndexOf((Accion)a);
			if (index >= 0)
			   return estados_hijos[index];
			else
			   return null;
		 }

		 public override Accion_MDP[] accionesValidas() {
			return acciones_hijos.ToArray();
		 }

		 public override Estado_MDP[] proximosEstados() {
			return estados_hijos.ToArray();
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
	  public Dictionary<int, Dictionary<Vector2, List<Nodo_Estado>>> estados_dict;
	  public List<Nodo_Estado> estados;
	  public Nodo_Estado nodo_estado_inicial;
	  public Dictionary<int, Dictionary<Vector2, List<Nodo_Estado>>> frontera_dict;
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
		 estados_dict = new Dictionary<int, Dictionary<Vector2, List<Nodo_Estado>>>();
		 estados = new List<Nodo_Estado>();
		 frontera_dict = new Dictionary<int, Dictionary<Vector2, List<Nodo_Estado>>>();
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

	  private void AgregarEstadoDict(Nodo_Estado estado, Dictionary<int, Dictionary<Vector2, List<Nodo_Estado>>> dict) {
		 int cant_obj_cumplidos = estado.estado_actual.objetivos_cumplidos.Count;
		 Vector2 posicion_jugador_0 = estado.estado_actual.posicion_jugadores[0];

		 Dictionary<Vector2, List<Nodo_Estado>> PosJugador_Estados;
		 if (dict.TryGetValue(cant_obj_cumplidos, out PosJugador_Estados)) {
			List<Nodo_Estado> Lista_Estados;
			if (PosJugador_Estados.TryGetValue(posicion_jugador_0, out Lista_Estados)) {
			   Lista_Estados.Add(estado);
			}
			else {
			   Lista_Estados = new List<Nodo_Estado>();
			   Lista_Estados.Add(estado);
			   PosJugador_Estados.Add(posicion_jugador_0, Lista_Estados);
			}
		 }
		 else {
			PosJugador_Estados = new Dictionary<Vector2, List<Nodo_Estado>>();
			List<Nodo_Estado> Lista_Estados = new List<Nodo_Estado>();
			Lista_Estados.Add(estado);
			PosJugador_Estados.Add(posicion_jugador_0, Lista_Estados);
			dict.Add(cant_obj_cumplidos, PosJugador_Estados);
		 }
	  }

	  private bool GetEstadoDict(Nodo_Estado estado, Dictionary<int, Dictionary<Vector2, List<Nodo_Estado>>> dict, out Nodo_Estado nodo_estado_resultado) {
		 int cant_obj_cumplidos = estado.estado_actual.objetivos_cumplidos.Count;
		 Vector2 posicion_jugador_0 = estado.estado_actual.posicion_jugadores[0];

		 Dictionary<Vector2, List<Nodo_Estado>> PosJugador_Estados;
		 if (dict.TryGetValue(cant_obj_cumplidos, out PosJugador_Estados)) {
			List<Nodo_Estado> Lista_Estados;
			if (PosJugador_Estados.TryGetValue(posicion_jugador_0, out Lista_Estados)) {
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

	  private void RemoverEstadoDict(Nodo_Estado estado, Dictionary<int, Dictionary<Vector2, List<Nodo_Estado>>> dict) {
		 int cant_obj_cumplidos = estado.estado_actual.objetivos_cumplidos.Count;
		 Vector2 posicion_jugador_0 = estado.estado_actual.posicion_jugadores[0];

		 Dictionary<Vector2, List<Nodo_Estado>> PosJugador_Estados;
		 if (dict.TryGetValue(cant_obj_cumplidos, out PosJugador_Estados)) {
			List<Nodo_Estado> Lista_Estados;
			if (PosJugador_Estados.TryGetValue(posicion_jugador_0, out Lista_Estados)) {
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
			Juego.Zona zona = escenario_base[posicion_jugador.y][posicion_jugador.x];
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


	  public void ImprimirEscenario(int offsetx, int offsety) {
		 for (int i = 0; i < escenario_base.Length; i++) {
			for (int j = 0; j < escenario_base[i].Length; j++) {
			   ImprimirZona(j + offsety, i + offsetx, escenario_base[i][j]);
			}
		 }
	  }

	  public void ImprimirJugadores(int i, int j) {
		 foreach (Juego.Jugador jugador in jugadores) {
			TCODConsole.root.printEx(jugador.posicion.x + i, jugador.posicion.y + j, TCODBackgroundFlag.Multiply, TCODAlignment.CenterAlignment, "" + jugador.representacion);
		 }
	  }

	  public void ImprimirZona(int i, int j, Juego.Zona zona) {
		 TCODConsole.root.print(i, j, "" + zona.representacion);
		 TCODColor color_fondo, color_frente;
		 switch (zona.tipo) {
			case (Juego.Zona.TZona.LIMITE):
			   color_fondo = TCODColor.darkerGrey;
			   color_frente = TCODColor.darkestGrey;
			   color_frente.scaleHSV(1f, 0.8f);
			   break;
			case (Juego.Zona.TZona.PARED):
			   color_fondo = TCODColor.darkerGrey;
			   color_frente = TCODColor.darkestGrey;
			   color_frente.scaleHSV(1f, 0.8f);
			   break;
			case (Juego.Zona.TZona.PISO):
			   color_fondo = TCODColor.lightGrey;
			   color_frente = TCODColor.lightGrey.Multiply(1.35f);
			   color_frente.scaleHSV(1f, 0.8f);
			   break;
			case (Juego.Zona.TZona.OBJETIVO):
			   color_fondo = TCODColor.darkYellow;
			   color_frente = TCODColor.black;
			   break;
			default:
			   color_fondo = TCODColor.darkGrey;
			   color_frente = TCODColor.lightGrey;
			   break;
		 }
		 TCODConsole.root.setCharBackground(i, j, color_fondo);
		 TCODConsole.root.setCharForeground(i, j, color_frente);
	  }

   }
}
