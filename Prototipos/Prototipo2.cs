using System;
using System.Collections.Generic;
using System.Diagnostics;
using libtcod;

namespace PruebasMarkov2 {
   public struct Vector2 {
	  public int x;
	  public int y;

	  public static Vector2 cero = new Vector2(0, 0);

	  public Vector2(int i, int j) {
		 x = i;
		 y = j;
	  }

	  public int distancia_directa(Vector2 posicion) {
		 return (int)Math.Sqrt(Math.Pow(x - posicion.x, 2) + Math.Pow(y - posicion.y, 2));
	  }

	  public int distancia_directa(int px, int py) {
		 return (int)Math.Sqrt(Math.Pow(x - px, 2) + Math.Pow(y - py, 2));
	  }

	  public override bool Equals(object obj) {
		 Vector2 vector = (Vector2)obj;
		 return ((vector.x == x) && (vector.y == y));
	  }

	  public override int GetHashCode() {
		 int multiplicador = (int)Math.Pow(10, Math.Floor(Math.Log10(Juego.alto)) + 1);
		 return (x * multiplicador + y);
	  }

	  public override string ToString() {
		 return "(" + x + "," + y + ")";
	  }
   }

   public class Juego {
	  public class Zona {
		 public enum TZona {
			PISO = (int)42, PARED = (int)248, LIMITE = (int)176, OBJETIVO = '?'
		 }
		 public enum TMovilidad {
			PASABLE, IMPASABLE
		 }

		 public TZona tipo;
		 public TMovilidad movilidad;
		 public char representacion;

		 public Zona(TZona t, TMovilidad m) {
			tipo = t;
			movilidad = m;
			representacion = (char)t;
		 }

		 public override string ToString() {
			return "Tipo: " + tipo + ", movilidad: " + movilidad;
		 }
	  }
	  public class Objetivo : Zona, Objetivo_MDP {
		 public int id;
		 public bool cumplido;
		 public Vector2 posicion;
		 public Objetivo complementario;

		 public Objetivo(int i, char r, Vector2 p, Objetivo c)
			: base(TZona.OBJETIVO, TMovilidad.PASABLE) {
			id = i;
			cumplido = false;
			representacion = r;
			posicion = p;
			complementario = c;
			if (complementario != null)
			   complementario.complementario = this;
		 }


		 public int GetID() {
			return id;
		 }

		 public override string ToString() {
			return "Objetivo_id: " + id + ", cumplido: " + cumplido + ", complementario_id: " + complementario.id;
		 }
	  }

	  public class Jugador {
		 public enum TControl {
			DIRECTO, IA
		 }

		 public int id;
		 public string nombre;
		 public char representacion;
		 public Vector2 posicion;
		 public TControl control;
		 // <turno, accion>
		 public Dictionary<int, Accion> acciones;

		 public Jugador(int i, string n, char r, Vector2 p, TControl c) {
			id = i;
			nombre = n;
			representacion = r;
			posicion = p;
			control = c;
			acciones = new Dictionary<int, Accion>();
		 }

		 public Accion RegistrarAccion(int turno, Accion accion) {
			acciones.Add(turno, accion);
			return accion;
		 }

		 public override string ToString() {
			return "Jugado_id: " + id + ", nombre: " + nombre + ", posicion: " + posicion;
		 }
	  }

	  public static int ancho, alto;

	  public int alto_ventana_acciones;
	  public Zona[][] escenario;
	  public Jugador[] jugadores;
	  public Objetivo[] objetivos;

	  private int offsetx;
	  private int offsety;
	  private int ancho_ventana;
	  private int alto_ventana;

	  public TCODMap mapa_fov;

	  public List<Accion> acciones_posibles;
	  public List<string> log_acciones;

	  public Arbol_Estados arbol_estados;
	  public ResolucionMDP resolucion;
	  public Arbol_Estados.Nodo_Estado nodo_estado_actual;
	  // <turno, estado>
	  public Dictionary<int, Arbol_Estados.Nodo_Estado> historial_estados;
	  public int turno;

	  public Juego(int an, int al, int nj, int no) {
		 ancho = an;
		 alto = al;

		 offsetx = 2;
		 offsety = 2;
		 ancho_ventana = 80;
		 alto_ventana = 45;
		 alto_ventana_acciones = alto_ventana - (offsety + alto + 4);

		 log_acciones = new List<string>();

		 PrepararPantalla(15, ancho_ventana, alto_ventana);

		 PrepararObjetivos(no);
		 PrepararEscenario(nj);
		 //PrepararJugadores(nj);
		 PrepararAcciones();
		 PrepararMapaFov();

		 arbol_estados = new Arbol_Estados(ref escenario, ref jugadores, ref acciones_posibles, ref objetivos);
		 resolucion = new ResolucionMDP(ref arbol_estados);
		 nodo_estado_actual = arbol_estados.nodo_estado_inicial;
		 historial_estados = new Dictionary<int, Arbol_Estados.Nodo_Estado>();
		 turno = 0;
		 historial_estados.Add(turno, nodo_estado_actual);

		 // Gameloop.
		 while (!TCODConsole.isWindowClosed()) {
			// Controles.
			//TCODKey tecla = TCODConsole.checkForKeypress((int)TCODKeyStatus.KeyPressed);
			TCODKey tecla = TCODConsole.waitForKeypress(false);
			if (tecla.LeftAlt && tecla.KeyCode == TCODKeyCode.Enter) {
			   TCODConsole.setFullscreen(!TCODConsole.isFullscreen());
			}
			if (tecla.KeyCode == TCODKeyCode.Escape)
			   break;

			// Acciones a realizar.
			Accion[] acciones = BuscarAcciones(tecla);
			RealizarAcciones(acciones);
			VerificarCumplimientoObjetivos();

			// Impresion por pantalla.
			TCODConsole.root.setBackgroundColor(TCODColor.darkerGrey);
			TCODConsole.root.clear();

			ImprimirGUI();
			ImprimirEscenario(offsetx, offsety);
			ImprimirJugadores(offsety, offsetx);
			ImprimirFov(offsetx, offsety, 8);

			TCODConsole.flush();
		 }
	  }

	  // Preparacion.

	  public void PrepararPantalla(int fps, int anv, int alv) {
		 TCODSystem.setFps(fps);
		 TCODConsole.setCustomFont("./Fonts/12x12.png", (int)(TCODFontFlags.LayoutAsciiInRow | TCODFontFlags.Grayscale), 16, 16);
		 TCODConsole.initRoot(anv, alv, "Pruebas", false, TCODRendererType.SDL);
	  }

	  public Accion BuscarAccion(Jugador j, TAccion ta, TDireccion td) {
		 foreach (Accion accion in acciones_posibles) {
			if (accion.jugador == j && accion.tipo == ta && accion.direccion == td)
			   return accion;
		 }
		 return null;
	  }

	  public void PrepararAcciones() {
		 acciones_posibles = new List<Accion>();
		 int i = 0;
		 foreach (TDireccion td in typeof(TDireccion).GetEnumValues()) {
			foreach (Jugador jugador in jugadores) {
			   acciones_posibles.Add(new Accion(i, jugador, TAccion.MOVIMIENTO, td));
			   i++;
			}
		 }

		 foreach (Accion accion in acciones_posibles) {
			accion.complementaria = BuscarAccion(accion.jugador, accion.tipo, Accion.direccion_complementaria[accion.direccion]);
		 }
	  }

	  public void PrepararObjetivos(int n) {
		 objetivos = new Objetivo[n];
		 char[] letras = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G' };
		 for (int i = 0; i < n; i += 2) {
			Objetivo objetivoA = new Objetivo(i, letras[i / 2], Vector2.cero, null);
			Objetivo objetivoB = new Objetivo(i + 1, char.ToLower(letras[i / 2]), Vector2.cero, objetivoA);
			objetivos[i] = objetivoA;
			objetivos[i + 1] = objetivoB;
		 }
	  }

	  public void PrepararJugadores(int n) {
		 jugadores = new Jugador[n];
		 Random R = new Random();

		 jugadores[0] = new Jugador(0, "Jugador_" + 0, '@', new Vector2(R.Next(1, ancho - 1), R.Next(1, alto - 1)), Jugador.TControl.DIRECTO);
		 while (escenario[jugadores[0].posicion.y][jugadores[0].posicion.x].movilidad == Zona.TMovilidad.IMPASABLE)
			jugadores[0].posicion = new Vector2(R.Next(1, ancho - 1), R.Next(1, alto - 1));

		 for (int q = 1; q < n; q++) {
			jugadores[q] = new Jugador(q, "Jugador_" + q, '$', new Vector2(R.Next(1, ancho - 1), R.Next(1, alto - 1)), Jugador.TControl.IA);
			while (escenario[jugadores[q].posicion.y][jugadores[q].posicion.x].movilidad == Zona.TMovilidad.IMPASABLE)
			   jugadores[q].posicion = new Vector2(R.Next(1, ancho - 1), R.Next(1, alto - 1));
		 }
	  }

	  public void PrepararEscenario(int nj) {
		 bool continuar = false;
		 while (!continuar) {
			escenario = Generador_Escenario.generarEscenario(ancho, alto, objetivos);
			PrepararJugadores(nj);
			TCODConsole.root.setBackgroundColor(TCODColor.darkestGrey);
			TCODConsole.root.clear();
			ImprimirEscenario(2, 2);
			ImprimirJugadores(2, 2);
			TCODConsole.flush();
			continuar = (TCODConsole.waitForKeypress(true).KeyCode == TCODKeyCode.Space);
		 }
	  }

	  public void PrepararMapaFov() {
		 mapa_fov = new TCODMap(ancho, alto);
		 for (int i = 0; i < alto; i++) {
			for (int j = 0; j < ancho; j++) {
			   bool pasable = (escenario[i][j].movilidad == Zona.TMovilidad.PASABLE);
			   mapa_fov.setProperties(j, i, pasable, pasable);
			}
		 }
	  }

	  // Acciones.

	  public Objetivo InferirObjetivo(Jugador jugador, int turnos, float factor_d, out float[] valor_objetivo) {
		 valor_objetivo = new float[objetivos.Length];
		 float descuento = 1.0f;
		 float suma = 0;
		 Arbol_Estados.Nodo_Estado nodo;
		 int tope_inferior = turno - Math.Min(turnos, turno);
		 for (int t = turno; t >= tope_inferior; t--) {
			if (historial_estados.TryGetValue(t, out nodo)) {
			   foreach (Objetivo objetivo in objetivos) {
				  Accion accion = resolucion.mdp.Politica[jugador.id][objetivo.id][nodo.estado_actual.id];
				  Accion accion_jugador;
				  if (jugador.acciones.TryGetValue(t, out accion_jugador) && accion.id == accion_jugador.id) {
					 valor_objetivo[objetivo.id] += 1000 * descuento;
					 suma += 1000 * descuento;
				  }
			   }
			}
			descuento = descuento * factor_d;
		 }

		 if (suma > 0) {
			for (int i = 0; i < valor_objetivo.Length; i++) {
			   valor_objetivo[i] = valor_objetivo[i] / suma;
			}

			float max_valor = float.MinValue;
			int objetivo_id = -1;
			for (int i = 0; i < valor_objetivo.Length; i++) {
			   //Console.WriteLine("Objetivo: " + objetivos[i].representacion + ", valor: " + valor_objetivo[i] + ".");
			   if (valor_objetivo[i] > max_valor) {
				  max_valor = valor_objetivo[i];
				  objetivo_id = i;
			   }
			}
			//Console.WriteLine();

			if (objetivo_id != -1) {
			   return objetivos[objetivo_id];
			}
			else {
			   return null;
			}
		 }
		 else {
			return null;
		 }
	  }

	  public void VerificarCumplimientoObjetivos() {
		 bool[] objetivos_pisados = new bool[objetivos.Length];
		 foreach (Jugador jugador in jugadores) {
			Juego.Zona zona = escenario[jugador.posicion.y][jugador.posicion.x];
			if (zona.tipo == Juego.Zona.TZona.OBJETIVO) {
			   Juego.Objetivo objetivo = (Juego.Objetivo)zona;
			   objetivos_pisados[objetivo.id] = true;
			}
		 }

		 foreach (Objetivo objetivo in objetivos) {
			if (objetivos_pisados[objetivo.id] && objetivos_pisados[objetivo.complementario.id]) {
			   objetivo.cumplido = true;
			}
		 }
	  }

	  public Accion[] BuscarAcciones(TCODKey key) {
		 Accion[] acciones_realizadas = new Accion[jugadores.Length];
		 Accion accion_realizada;
		 turno++;
		 for (int i = 0; i < jugadores.Length; i++) {
			Jugador jugador = jugadores[i];
			accion_realizada = null;
			switch (jugador.control) {
			   case Jugador.TControl.DIRECTO:
				  if (turno % jugadores.Length == 0) {
					 foreach (Accion accion in acciones_posibles) {
						if ((char)accion.direccion == key.Character && accion.jugador.id == jugador.id) {
						   accion_realizada = accion;
						}
					 }
					 acciones_realizadas[i] = accion_realizada;
				  }
				  else
					 acciones_realizadas[i] = null;
				  break;
			   case Jugador.TControl.IA:
				  // TODO.
				  Random R = new Random();
				  if (turno % jugadores.Length == 1) {
					 acciones_realizadas[i] = resolucion.mdp.Politica[jugador.id][R.Next(0, objetivos.Length)][nodo_estado_actual.estado_actual.id];
				  }
				  else
					 acciones_realizadas[i] = null;
				  break;
			   default:
				  // TODO.
				  acciones_realizadas[i] = null;
				  break;
			}
		 }

		 return acciones_realizadas;
	  }

	  public bool IntentarMovimiento(Jugador jugador, TDireccion direccion) {
		 Vector2 posicion_deseada = jugador.posicion;
		 switch (direccion) {
			case TDireccion.UL:
			   posicion_deseada.x--;
			   posicion_deseada.y--;
			   break;
			case TDireccion.UR:
			   posicion_deseada.x++;
			   posicion_deseada.y--;
			   break;
			case TDireccion.U:
			   posicion_deseada.y--;
			   break;
			case TDireccion.DL:
			   posicion_deseada.x--;
			   posicion_deseada.y++;
			   break;
			case TDireccion.DR:
			   posicion_deseada.x++;
			   posicion_deseada.y++;
			   break;
			case TDireccion.D:
			   posicion_deseada.y++;
			   break;
			case TDireccion.L:
			   posicion_deseada.x--;
			   break;
			case TDireccion.R:
			   posicion_deseada.x++;
			   break;
		 }
		 if (escenario[posicion_deseada.y][posicion_deseada.x].movilidad == Zona.TMovilidad.PASABLE) {
			foreach (Jugador otro_jugador in jugadores) {
			   if ((otro_jugador != jugador) && (otro_jugador.posicion.Equals(posicion_deseada)))
				  return false;
			}
			jugador.posicion = posicion_deseada;
			return true;
		 }
		 else {
			return false;
		 }
	  }

	  public void RealizarAcciones(Accion[] accs) {
		 for (int i = 0; i < accs.Length; i++) {
			Accion accion = accs[i];
			bool exito = false;
			if (accion != null) {
			   switch (accion.tipo) {
				  case TAccion.MOVIMIENTO:
					 if (IntentarMovimiento(jugadores[i], accion.direccion)) {
						jugadores[i].RegistrarAccion(turno, accion);

						Debug.Assert(nodo_estado_actual.hijoAccion(accion) != null);

						nodo_estado_actual = (Arbol_Estados.Nodo_Estado)nodo_estado_actual.hijoAccion(accion);
						historial_estados.Add(turno, nodo_estado_actual);
						exito = true;
					 }
					 break;
				  default:
					 // TODO.
					 break;
			   }
			   string exito_string = exito ? "con exito" : "sin exito";
			   log_acciones.Add("El jugador " + jugadores[i].nombre + " realizo la accion " + accion.tipo + " en direccion " + accion.direccion + " " + exito_string + ".");
			   if (log_acciones.Count > alto_ventana_acciones)
				  log_acciones.RemoveAt(0);
			}
		 }
	  }

	  // Pantalla.

	  public string GetAccionesLog() {
		 string log_acciones_string = "";
		 foreach (string log_accion in log_acciones) {
			log_acciones_string += log_accion + "\n";
		 }
		 return log_acciones_string;
	  }

	  public string GetInformacion() {
		 string informacion;

		 informacion = "Posicion de jugadores:\n";
		 foreach (Jugador jugador in jugadores) {
			informacion += "  " + jugador.nombre + ": " + jugador.posicion.ToString() + "\n";
		 }

		 informacion += "Acciones posibles:\n";
		 foreach (Jugador jugador in jugadores) {
			informacion += "  " + jugador.nombre + "\n";
			informacion += "  ";
			foreach (Accion accion in nodo_estado_actual.acciones_hijos) {
			   if (accion.jugador.id == jugador.id) {
				  bool impreso = false;
				  foreach (Objetivo objetivo in objetivos) {
					 if (resolucion.mdp.Politica[jugador.id][objetivo.id][nodo_estado_actual.estado_actual.id] == accion) {
						informacion += accion.direccion + "(" + objetivo.representacion + ") ";
						impreso = true;
					 }
				  }

				  if (!impreso)
					 informacion += accion.direccion + " ";
			   }
			}
			informacion += "\n";
		 }

		 informacion += "Inferencia de objetivos:\n";
		 float[] valores;
		 foreach (Jugador jugador in jugadores) {
			InferirObjetivo(jugador, 10, 0.75f, out valores);
			informacion += "  " + jugador.nombre + ":\n";
			foreach (Objetivo objetivo in objetivos) {
			   informacion += objetivo.representacion + " (" + Math.Round(valores[objetivo.id], 2) + ") ";
			}
			informacion += "\n";
		 }

		 return informacion;
	  }

	  public void ImprimirGUI() {
		 TCODConsole.root.printFrame(0, 0, ancho_ventana, alto_ventana, false, TCODBackgroundFlag.Alpha, "Pruebas Markov");
		 TCODConsole.root.printFrame(offsety - 1, offsetx - 1, ancho + 2, alto + 2, false, TCODBackgroundFlag.Alpha, "Escenario");

		 TCODConsole.root.printFrame(offsetx + ancho + 1, offsety - 1, ancho_ventana - (offsetx + ancho + 2), alto + 2, false, TCODBackgroundFlag.Alpha, "Informacion");
		 TCODConsole.root.printRect(offsetx + ancho + 2, offsety, ancho_ventana - (offsetx + ancho + 3), alto + 1, GetInformacion());

		 TCODConsole.root.printFrame(offsetx - 1, offsety + alto + 1, ancho_ventana - 2, alto_ventana - (offsety + alto + 2), false, TCODBackgroundFlag.Alpha, "Acciones");
		 TCODConsole.root.printRect(offsetx, offsety + alto + 2, ancho_ventana - 3, alto_ventana - (offsety + alto + 3), GetAccionesLog());
	  }

	  public void ImprimirFov(int offsetx, int offsety, int radio) {
		 TCODColor[] colores = new TCODColor[] { TCODColor.darkRed, TCODColor.darkYellow, TCODColor.darkBlue, TCODColor.darkGreen };
		 foreach (Jugador jugador in jugadores) {
			mapa_fov.computeFov(jugador.posicion.x, jugador.posicion.y, radio, true);

			for (int i = 0; i < alto; i++) {
			   for (int j = 0; j < ancho; j++) {
				  bool en_fov = mapa_fov.isInFov(j, i);
				  if (en_fov) {
					 int dist_cuadrado = (int)(Math.Pow(jugador.posicion.y - i, 2) + Math.Pow(jugador.posicion.x - j, 2));
					 double intensidad_coef1 = 1.0f / (1.0f + dist_cuadrado / (radio / TCODRandom.getInstance().getGaussianRangeFloat(1, 2)));
					 double intensidad_coef2 = intensidad_coef1 - 1.0f / (1.0f + radio * radio);
					 double intensidad_coef3 = intensidad_coef2 / (1.0f - 1.0f / (1.0f + radio * radio));
					 TCODColor color_llama = TCODColor.Interpolate(colores[jugador.id * jugadores.Length], colores[jugador.id * jugadores.Length + 1], (float)intensidad_coef3);
					 TCODColor color = TCODColor.Interpolate(TCODColor.black, color_llama, (float)intensidad_coef3);
					 if (escenario[i][j].tipo == Zona.TZona.PARED || escenario[i][j].tipo == Zona.TZona.LIMITE)
						TCODConsole.root.setCharBackground(j + offsety, i + offsetx, color.Multiply(0.25f), TCODBackgroundFlag.Add);
					 else
						TCODConsole.root.setCharBackground(j + offsety, i + offsetx, color, TCODBackgroundFlag.Add);
				  }
			   }
			}
		 }
	  }

	  public void ImprimirEscenario(int offsetx, int offsety) {
		 for (int i = 0; i < alto; i++) {
			for (int j = 0; j < ancho; j++) {
			   ImprimirZona(j + offsety, i + offsetx, escenario[i][j]);
			}
		 }
	  }

	  public void ImprimirJugadores(int i, int j) {
		 foreach (Jugador jugador in jugadores) {
			TCODConsole.root.printEx(jugador.posicion.x + i, jugador.posicion.y + j, TCODBackgroundFlag.Burn, TCODAlignment.CenterAlignment, "" + jugador.representacion);
			TCODConsole.root.setCharForeground(jugador.posicion.x + i, jugador.posicion.y + j, TCODColor.white);
		 }
	  }

	  public void ImprimirZona(int i, int j, Zona zona) {
		 TCODConsole.root.print(i, j, "" + zona.representacion);
		 TCODColor color_fondo, color_frente;
		 switch (zona.tipo) {
			case (Zona.TZona.LIMITE):
			   color_fondo = TCODColor.darkerGrey;
			   color_frente = TCODColor.darkestGrey;
			   color_frente.scaleHSV(1f, 0.8f);
			   break;
			case (Zona.TZona.PARED):
			   color_fondo = TCODColor.darkerGrey;
			   color_frente = TCODColor.darkestGrey;
			   color_frente.scaleHSV(1f, 0.8f);
			   break;
			case (Zona.TZona.PISO):
			   color_fondo = TCODColor.darkerGrey;
			   color_frente = TCODColor.darkerGrey.Multiply(1.35f);
			   color_frente.scaleHSV(1f, 0.8f);
			   break;
			case (Zona.TZona.OBJETIVO):
			   Objetivo objetivo = (Objetivo)zona;
			   if (!objetivo.cumplido) {
				  color_fondo = TCODColor.darkGreen;
				  color_frente = TCODColor.black;
			   }
			   else {
				  color_fondo = TCODColor.darkYellow;
				  color_frente = TCODColor.black;
			   }
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

   class Program {
	  static void Main(string[] args) {
		 Accion.direccion_complementaria = new Dictionary<TDireccion, TDireccion>();
		 Accion.direccion_complementaria.Add(TDireccion.C, TDireccion.C);
		 Accion.direccion_complementaria.Add(TDireccion.D, TDireccion.U);
		 Accion.direccion_complementaria.Add(TDireccion.L, TDireccion.R);
		 Accion.direccion_complementaria.Add(TDireccion.UL, TDireccion.DR);
		 Accion.direccion_complementaria.Add(TDireccion.UR, TDireccion.DL);
		 Accion.direccion_complementaria.Add(TDireccion.U, TDireccion.D);
		 Accion.direccion_complementaria.Add(TDireccion.R, TDireccion.L);
		 Accion.direccion_complementaria.Add(TDireccion.DR, TDireccion.UL);
		 Accion.direccion_complementaria.Add(TDireccion.DL, TDireccion.UR);

		 int ancho = 16;
		 int alto = 16;

		 Juego juego = new Juego(ancho, alto, 2, 4);
	  }
   }
}
