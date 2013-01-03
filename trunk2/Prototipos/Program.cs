using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PruebasMarkov {
   [Serializable]
   public abstract class Estado {
	  public int id;

	  public abstract Estado[] proximosEstados();
	  public abstract Accion[] accionesValidas();
	  public abstract Estado transicionAccion(Accion a);
   }
   [Serializable]
   public abstract class Accion {
	  public int id;
   }
   [Serializable]
   public abstract class Transicion<S, A> {
	  public abstract float valor(A a, S s, S sp);
   }
   [Serializable]
   public abstract class Recompensa<S> {
	  public abstract float valor(S s);
   }

   public class MDP<S, A, T, R>
	  where S : Estado
	  where A : Accion
	  where T : Transicion<S, A>
	  where R : Recompensa<S> {
	  public List<S> estados;
	  public List<A> acciones;
	  public T transicion;
	  public R recompensa;
	  public float factor_descuento;

	  public float[] Utilidad;
	  public A[] Politica;

	  public MDP(S[] est, A[] acs, T trn, R rep, float fac) {
		 estados = new List<S>(est);
		 for (int i = 0; i < estados.Count; i++)
			estados[i].id = i;
		 acciones = new List<A>(acs);
		 for (int j = 0; j < acciones.Count; j++)
			acciones[j].id = j;
		 transicion = trn;
		 recompensa = rep;
		 factor_descuento = fac;

		 //Calcular_Utilidad_VI();
		 Calcular_Utilidad_PI();
	  }

	  public void Calcular_Utilidad_PI() {
		 float[] Utilidad_Aux = new float[estados.Count];
		 A[] Politica_Aux = new A[estados.Count];
		 float[] Value_Policy = new float[estados.Count];

		 Random R = new Random();
		 foreach (S i in estados) {
			Utilidad_Aux[i.id] = recompensa.valor(i);
			A[] acciones_validas = (A[])i.accionesValidas();
			if (acciones_validas.Length > 0)
			   Politica_Aux[i.id] = acciones_validas[R.Next(0, i.accionesValidas().Length)];
		 }

		 bool sincambios;
		 do {
			// Value_Determination
			foreach (S i in estados) {
			   Utilidad_Aux[i.id] = recompensa.valor(i) + Value_Policy[i.id];
			}
			sincambios = true;

			foreach (S i in estados) {
			   A action_max = null;
			   float value_max = float.MinValue;
			   foreach (A a in i.accionesValidas()) {
				  float value = 0;
				  foreach (S j in i.proximosEstados()) {
					 value += transicion.valor(a, i, j) * Utilidad_Aux[j.id];
				  }
				  if (value > value_max) {
					 value_max = value;
					 action_max = a;
				  }
			   }

			   float value_policy = 0;
			   foreach (S j in i.proximosEstados()) {
				  value_policy += transicion.valor(Politica_Aux[i.id], i, j) * Utilidad_Aux[j.id];
			   }

			   if (value_max > value_policy) {
				  Politica_Aux[i.id] = action_max;
				  sincambios = false;
				  Value_Policy[i.id] = value_max;
			   }
			   else {
				  Value_Policy[i.id] = value_policy;
			   }

			   Console.WriteLine("Progreso: " + (i.id * 100f / estados.Count));
			}
		 } while (!sincambios);

		 Utilidad = Utilidad_Aux;
		 Politica = Politica_Aux;
	  }


	  public void Calcular_Utilidad_VI() {
		 Utilidad = new float[estados.Count];
		 float[] Utilidad_Aux = new float[estados.Count];
		 A[] Politica_Aux = new A[estados.Count];

		 foreach (S i in estados) {
			Utilidad_Aux[i.id] = recompensa.valor(i);
		 }

		 do {
			Array.Copy(Utilidad_Aux, Utilidad, Utilidad_Aux.Length);

			int count = 0;
			foreach (S i in estados) {
			   float value_max = float.MinValue;
			   foreach (A a in i.accionesValidas()) {
				  float value = 0;
				  foreach (S j in i.proximosEstados()) {
					 value += transicion.valor(a, i, j) * Utilidad_Aux[j.id];
				  }
				  if (value > value_max) {
					 value_max = value;
					 Politica_Aux[i.id] = a;
				  }
				  count++;
				  Console.WriteLine("Completado: " + (count * 1f / (estados.Count * acciones.Count)));
			   }

			   Utilidad_Aux[i.id] = recompensa.valor(i) + value_max;
			}

		 } while (!similares(Utilidad_Aux, Utilidad, 0.2f));

		 Utilidad = Utilidad_Aux;
		 Politica = Politica_Aux;
	  }

	  public bool similares(float[] a, float[] b, float delta) {
		 double suma = 0;
		 foreach (S i in estados) {
			suma += Math.Pow(a[i.id] - b[i.id], 2);
		 }

		 double rms = Math.Sqrt(suma) / estados.Count;
		 Console.WriteLine(rms);
		 return (rms < delta);
	  }
   }

   public struct Posicion {
	  public int x, y;
   }

   [Serializable]
   public class EstadoJuego : Estado {
	  public List<EstadoJuego> padres;
	  public List<EstadoJuego> hijos;
	  public List<AccionJuego> acciones_padres;
	  public List<AccionJuego> acciones_hijos;

	  public bool estado_ganador;
	  public int ganador;

	  public int clave_horizonal;
	  public int clave_vertical;

	  public EstadoJuego()
		 : base() {

	  }

	  public override Estado transicionAccion(Accion a) {
		 int indice = acciones_hijos.IndexOf((AccionJuego)a);
		 if (indice >= 0) {
			return hijos[indice];
		 }
		 else {
			return null;
		 }
	  }

	  public override Estado[] proximosEstados() {
		 return hijos.ToArray();
	  }

	  public override Accion[] accionesValidas() {
		 return acciones_hijos.ToArray();
	  }

	  public static int clave(int jugador) {
		 switch (jugador) {
			case Juego.PA1:
			   return 1;
			case Juego.PA2:
			   return 1;
			case Juego.PB1:
			   return 2;
			case Juego.PB2:
			   return 2;
			case Juego.vacio:
			   return 4;
			default:
			   return 0;
		 }
	  }

	  public static int calcularClaveHorizontal(int[][] tablero) {
		 int valor = 0;
		 for (int i = 0; i < tablero.Length; i++) {
			int suma = 0;
			for (int j = 0; j < tablero[0].Length; j++)
			   suma += clave(tablero[i][j]);
			valor += suma * (int)Math.Pow(2, i * tablero[i].Length);
		 }

		 return valor;
	  }

	  public static int calcularClaveVertical(int[][] tablero) {
		 int[] suma = new int[tablero[0].Length];
		 for (int i = 0; i < tablero.Length; i++)
			for (int j = 0; j < tablero[0].Length; j++)
			   suma[j] += clave(tablero[i][j]);

		 int valor = 0;
		 for (int i = 0; i < tablero[0].Length; i++)
			valor += suma[i] * (int)Math.Pow(2, i * tablero.Length);

		 return valor;
	  }

	  public int calcularClaveHorizontal(int clavePadre, int p1y, int p2y, int p3y, int p4y, int ancho) {
		 int claveh = clavePadre;
		 claveh += (-3 * (int)Math.Pow(2, p1y * ancho)) + (-3 * (int)Math.Pow(2, p2y * ancho)) + (-2 * (int)Math.Pow(2, p3y * ancho)) + (-2 * (int)Math.Pow(2, p4y * ancho));
		 return claveh;
	  }

	  public int calcularClaveVertical(int clavePadre, int p1x, int p2x, int p3x, int p4x, int alto) {
		 int clavev = clavePadre;
		 clavev += (-3 * (int)Math.Pow(2, p1x * alto)) + (-3 * (int)Math.Pow(2, p2x * alto)) + (-2 * (int)Math.Pow(2, p3x * alto)) + (-2 * (int)Math.Pow(2, p4x * alto));
		 return clavev;
	  }

	  public int buscarDistanciaGanador(int maxdist, int equipo) {
		 if ((estado_ganador) && (ganador == equipo))
			return 0;
		 else if (maxdist > 1) {
			int menor_distancia = int.MaxValue - 1;
			foreach (EstadoJuego estadoh in hijos) {
			   int distancia = estadoh.buscarDistanciaGanador(maxdist - 1, equipo);
			   menor_distancia = Math.Min(menor_distancia, distancia);
			}
			return (menor_distancia + 1);
		 }
		 return int.MaxValue;
	  }
   }

   [Serializable]
   public class AccionJuego : Accion {
	  public int columnaPA1;
	  public int columnaPB1;
	  public int columnaPA2;
	  public int columnaPB2;

	  public AccionJuego(int p1, int p2, int p3, int p4) {
		 columnaPA1 = p1;
		 columnaPB1 = p2;
		 columnaPA2 = p3;
		 columnaPB2 = p4;
	  }
   }

   [Serializable]
   public class TransicionJuego : Transicion<EstadoJuego, AccionJuego> {
	  public override float valor(AccionJuego a, EstadoJuego s, EstadoJuego sp) {
		 if (sp.padres != null) {
			int indice = sp.padres.IndexOf(s);
			if ((indice > 0) && (sp.acciones_padres[indice] == a))
			   return 1f / s.hijos.Count;
			else
			   return 0;
		 }
		 else {
			return 0;
		 }
	  }
   }

   [Serializable]
   public class RecompensaJuego : Recompensa<EstadoJuego> {
	  public override float valor(EstadoJuego s) {
		 if (s.estado_ganador && s.ganador == Juego.EA) {
			return 1;
		 }
		 else if (s.estado_ganador && s.ganador == Juego.EB) {
			return -1;
		 }
		 else
			return 0;
	  }
   }

   public class Juego {
	  public const int PA1 = 100;
	  public const char SA1 = 'X';
	  public const int PA2 = 101;
	  public const char SA2 = 'x';
	  public const int PB1 = 200;
	  public const char SB1 = 'O';
	  public const int PB2 = 201;
	  public const char SB2 = 'o';
	  public const int vacio = 0;
	  public const int EA = 1;
	  public const int EB = 2;
	  public int[][] tablero;

	  public MDP<EstadoJuego, AccionJuego, TransicionJuego, RecompensaJuego> mdp;
	  public EstadoJuego estado_actual;
	  public AccionJuego accion_actual;

	  public int[] orden = new int[] { PA1, PB1, PA2, PB2 };
	  public int turno;

	  public Juego(int ancho, int alto) {
		 tablero = new int[alto][];
		 for (int i = 0; i < alto; i++) {
			tablero[i] = new int[ancho];
		 }
		 turno = 0;
		 accion_actual = new AccionJuego(0, 0, 0, 0);
	  }

	  public int equipo(int jugador) {
		 switch (jugador) {
			case PA1:
			   return EA;
			case PA2:
			   return EA;
			case PB1:
			   return EB;
			case PB2:
			   return EB;
			default:
			   return -1;
		 }
	  }

	  public int[][] duplicarTablero() {
		 int[][] copia_tablero = new int[tablero.Length][];
		 for (int i = 0; i < tablero.Length; i++) {
			copia_tablero[i] = new int[tablero[0].Length];
			for (int j = 0; j < tablero[0].Length; j++) {
			   copia_tablero[i][j] = tablero[i][j];
			}
		 }

		 return copia_tablero;
	  }

	  public bool chequearValidez(int col) {
		 return (tablero[0][col] == vacio);
	  }

	  public int esperarJugada() {
		 int col = int.Parse(Console.ReadLine());
		 return col;
	  }

	  public int hacerJugada(int col, int jugador) {
		 int fila = 0;
		 int fila_anterior = -1;
		 while ((fila < tablero.Length) && (tablero[fila][col] == vacio)) {
			fila_anterior = fila;
			fila++;
		 }
		 tablero[fila_anterior][col] = jugador;
		 return fila_anterior;
	  }


	  public AccionJuego buscar_accion(AccionJuego a) {
		 foreach (AccionJuego accion in mdp.acciones) {
			if ((accion.columnaPA1 == a.columnaPA1) && (accion.columnaPB1 == a.columnaPB1) && (accion.columnaPA2 == a.columnaPA2) && (accion.columnaPB2 == a.columnaPB2)) {
			   return accion;
			}
		 }
		 return null;
	  }

	  public int buscarJugada() {
		 AccionJuego max_accion = null;
		 float max_value = 0;
		 for (int i = 0; i < tablero[0].Length; i++) {
			accion_actual.columnaPA2 = i;
			AccionJuego accion = null;
			AccionJuego accion_valida = null;
			float value = 0;
			for (int j = 0; j < tablero[0].Length; j++) {
			   accion_actual.columnaPB2 = j;
			   accion = buscar_accion(accion_actual);
			   EstadoJuego proximo_estado = (EstadoJuego)estado_actual.transicionAccion(accion);
			   if (proximo_estado != null) {
				  value += mdp.Utilidad[proximo_estado.id];
				  accion_valida = accion;
			   }
			}
			if ((value >= max_value) && (accion_valida != null)) {
			   max_value = value;
			   max_accion = accion_valida;
			}
		 }
		 return max_accion.columnaPA2 + 1;
	  }

	  public bool chequearVictoria(int fila, int col) {
		 int bandojugado = equipo(tablero[fila][col]);
		 int maxcol = Math.Min(col + 3, tablero[0].Length - 1);
		 int mincol = Math.Max(col - 3, 0);
		 int maxfila = Math.Min(fila + 3, tablero.Length - 1);
		 int minfila = Math.Max(fila - 3, 0);

		 int secuencia = 0;

		 secuencia = 0;
		 for (int icol = mincol; icol <= maxcol; icol++) {
			if (equipo(tablero[fila][icol]) == bandojugado)
			   secuencia++;
			else
			   secuencia = 0;

			//Console.Write("[" + fila + ";" + icol + "]");

			if (secuencia >= 4)
			   return true;
		 }
		 //Console.WriteLine();

		 secuencia = 0;
		 for (int ifila = minfila; ifila <= maxfila; ifila++) {
			if (equipo(tablero[ifila][col]) == bandojugado)
			   secuencia++;
			else
			   secuencia = 0;

			//Console.Write("[" + ifila + ";" + col + "]");

			if (secuencia >= 4)
			   return true;
		 }
		 //Console.WriteLine();

		 secuencia = 0;
		 int jfila = fila - Math.Min(fila - minfila, col - mincol);
		 int jcol = col - Math.Min(fila - minfila, col - mincol);
		 for (int i = 0; i <= Math.Min(maxfila - fila, maxcol - col) + Math.Min(fila - minfila, col - mincol); i++) {
			if (equipo(tablero[jfila + i][jcol + i]) == bandojugado)
			   secuencia++;
			else
			   secuencia = 0;

			//Console.Write("[" + (jfila + i) + ";" + (jcol + i) + "]");

			if (secuencia >= 4)
			   return true;
		 }
		 //Console.WriteLine();


		 secuencia = 0;
		 jfila = fila + Math.Min(maxfila - fila, col - mincol);
		 jcol = col - Math.Min(maxfila - fila, col - mincol);
		 for (int i = 0; i <= Math.Min(fila - minfila, maxcol - col) + Math.Min(maxfila - fila, col - mincol); i++) {
			if (equipo(tablero[jfila - i][jcol + i]) == bandojugado)
			   secuencia++;
			else
			   secuencia = 0;

			//Console.Write("[" + (jfila - i) + ";" + (jcol + i) + "]");

			if (secuencia >= 4)
			   return true;
		 }
		 //Console.WriteLine();

		 return false;
	  }

	  public bool GameLoop() {
		 int mano = orden[turno % orden.Length];
		 int columna = -1;
		 bool jugada_valida = false;
		 Random R = new Random();
		 while (!jugada_valida) {
			switch (mano) {
			   case PA1:
				  columna = esperarJugada();
				  accion_actual.columnaPA1 = columna - 1;
				  break;
			   case PA2:
				  columna = buscarJugada();
				  accion_actual.columnaPA2 = columna - 1;
				  break;
			   case PB1:
				  columna = esperarJugada();
				  accion_actual.columnaPB1 = columna - 1;
				  break;
			   case PB2:
				  columna = R.Next(1, tablero[0].Length + 1);
				  accion_actual.columnaPB2 = columna - 1;
				  break;
			}

			jugada_valida = chequearValidez(columna - 1);
		 }

		 int fila = hacerJugada(columna - 1, mano);
		 turno++;
		 if ((turno % 4) == 0) {
			AccionJuego accion = buscar_accion(accion_actual);
			estado_actual = (EstadoJuego)estado_actual.transicionAccion(accion);
		 }
		 return chequearVictoria(fila, columna - 1);
	  }

	  public char getSimbolo(int v) {
		 switch (v) {
			case PA1:
			   return SA1;
			case PA2:
			   return SA2;
			case PB1:
			   return SB1;
			case PB2:
			   return SB2;
			default:
			   return ' ';
		 }
	  }

	  public void imprimirTablero() {
		 Console.WriteLine();
		 Console.WriteLine();

		 Console.Write("      ");
		 for (int col = 0; col < tablero[0].Length; col++) {
			Console.Write((col + 1) + " ");
		 }
		 Console.WriteLine();

		 for (int fil = 0; fil < tablero.Length; fil++) {
			Console.Write("     ");
			Console.Write("-");
			for (int col = 0; col < tablero[fil].Length; col++) {
			   Console.Write("--");
			}
			Console.WriteLine();

			Console.Write("   " + (fil + 1) + " ");
			for (int col = 0; col < tablero[fil].Length; col++) {
			   Console.Write("|");
			   Console.Write(getSimbolo(tablero[fil][col]));
			}
			Console.Write("|");
			Console.WriteLine();
		 }
		 Console.Write("     ");
		 Console.Write("-");
		 for (int col = 0; col < tablero[0].Length; col++) {
			Console.Write("--");
		 }
		 Console.WriteLine();
	  }
   }

   public class Resolucion {
	  public int ancho_tablero;
	  public int alto_tablero;
	  public Juego juego;

	  public Resolucion(int ancho, int alto) {
		 ancho_tablero = ancho;
		 alto_tablero = alto;
		 juego = new Juego(ancho, alto);
	  }

	  public AccionJuego[] prepararAcciones() {
		 AccionJuego[] acciones = new AccionJuego[(int)Math.Pow(ancho_tablero, 4)];
		 int indice = 0;
		 for (int i = 0; i < ancho_tablero; i++) {
			for (int j = 0; j < ancho_tablero; j++) {
			   for (int q = 0; q < ancho_tablero; q++) {
				  for (int k = 0; k < ancho_tablero; k++) {
					 acciones[indice] = new AccionJuego(i, j, q, k);
					 indice++;
				  }
			   }
			}
		 }
		 return acciones;
	  }

	  public EstadoJuego[] prepararEstados(AccionJuego[] acciones) {
		 List<EstadoJuego> estados_list = new List<EstadoJuego>();
		 Dictionary<int, Dictionary<int, EstadoJuego>> estados = new Dictionary<int, Dictionary<int, EstadoJuego>>();
		 EstadoJuego estado_inicial = new EstadoJuego();
		 estado_inicial.padres = null;
		 estado_inicial.hijos = new List<EstadoJuego>();
		 estado_inicial.acciones_hijos = new List<AccionJuego>();
		 estado_inicial.acciones_padres = null;
		 estado_inicial.estado_ganador = false;

		 Juego juego_base = new Juego(ancho_tablero, alto_tablero);

		 Queue<EstadoJuego> cola_estados = new Queue<EstadoJuego>();
		 Queue<int[][]> cola_juegos = new Queue<int[][]>();
		 cola_estados.Enqueue(estado_inicial);
		 cola_juegos.Enqueue(juego_base.tablero);

		 Dictionary<int, EstadoJuego> llave_estado = new Dictionary<int, EstadoJuego>();
		 int claveVertical, claveHorizontal;
		 claveVertical = EstadoJuego.calcularClaveVertical(juego_base.tablero);
		 claveHorizontal = EstadoJuego.calcularClaveHorizontal(juego_base.tablero);
		 estado_inicial.clave_horizonal = claveHorizontal;
		 estado_inicial.clave_vertical = claveVertical;
		 llave_estado.Add(claveHorizontal, estado_inicial);
		 estados.Add(claveVertical, llave_estado);
		 estados_list.Add(estado_inicial);

		 int contador = 0;
		 while (cola_estados.Count > 0) {
			EstadoJuego estado_actual = cola_estados.Dequeue();
			int[][] juego_actual = cola_juegos.Dequeue();
			foreach (AccionJuego accion in acciones) {
			   juego_base.tablero = juego_actual;
			   int[][] juego_hijo = juego_base.duplicarTablero();
			   juego_base.tablero = juego_hijo;

			   int filaPA1, filaPA2, filaPB1, filaPB2;
			   bool estado_ganador = false;
			   int ganador = 0;

			   if (juego_base.chequearValidez(accion.columnaPA1)) {
				  filaPA1 = juego_base.hacerJugada(accion.columnaPA1, Juego.PA1);
				  if (!estado_ganador) {
					 estado_ganador = juego_base.chequearVictoria(filaPA1, accion.columnaPA1);
					 ganador = Juego.EA;
				  }
			   }
			   else
				  continue;

			   if (juego_base.chequearValidez(accion.columnaPB1)) {
				  filaPB1 = juego_base.hacerJugada(accion.columnaPB1, Juego.PB1);
				  if (!estado_ganador) {
					 estado_ganador = juego_base.chequearVictoria(filaPB1, accion.columnaPB1);
					 ganador = Juego.EB;
				  }
			   }
			   else
				  continue;

			   if (juego_base.chequearValidez(accion.columnaPA2)) {
				  filaPA2 = juego_base.hacerJugada(accion.columnaPA2, Juego.PA2);
				  if (!estado_ganador) {
					 estado_ganador = juego_base.chequearVictoria(filaPA2, accion.columnaPA2);
					 ganador = Juego.EA;
				  }
			   }
			   else
				  continue;

			   if (juego_base.chequearValidez(accion.columnaPB2)) {
				  filaPB2 = juego_base.hacerJugada(accion.columnaPB2, Juego.PB2);
				  if (!estado_ganador) {
					 estado_ganador = juego_base.chequearVictoria(filaPB2, accion.columnaPB1);
					 ganador = Juego.EB;
				  }
			   }
			   else
				  continue;

			   EstadoJuego estado_hijo = null;
			   claveHorizontal = estado_actual.calcularClaveHorizontal(estado_actual.clave_horizonal, filaPA1, filaPA2, filaPB1, filaPB2, ancho_tablero);
			   claveVertical = estado_actual.calcularClaveVertical(estado_actual.clave_vertical, accion.columnaPA1, accion.columnaPA2, accion.columnaPB1, accion.columnaPB2, alto_tablero);
			   if (!estados.TryGetValue(claveVertical, out llave_estado)) {
				  llave_estado = new Dictionary<int, EstadoJuego>();
				  estados.Add(claveVertical, llave_estado);
			   }
			   if (!llave_estado.TryGetValue(claveHorizontal, out estado_hijo)) {
				  estado_hijo = new EstadoJuego();
				  estado_hijo.padres = new List<EstadoJuego>();
				  estado_hijo.hijos = new List<EstadoJuego>();
				  estado_hijo.acciones_hijos = new List<AccionJuego>();
				  estado_hijo.acciones_padres = new List<AccionJuego>();
				  estado_hijo.clave_horizonal = claveHorizontal;
				  estado_hijo.clave_vertical = claveVertical;
				  estado_hijo.estado_ganador = estado_ganador;
				  estado_hijo.ganador = ganador;

				  estados_list.Add(estado_hijo);
				  cola_estados.Enqueue(estado_hijo);
				  cola_juegos.Enqueue(juego_base.tablero);
				  llave_estado.Add(claveHorizontal, estado_hijo);
			   }

			   estado_actual.hijos.Add(estado_hijo);
			   estado_actual.acciones_hijos.Add(accion);
			   estado_hijo.padres.Add(estado_actual);
			   estado_hijo.acciones_padres.Add(accion);
			}
			contador++;
			Console.WriteLine("Estados procesados: " + contador);

		 }

		 return estados_list.ToArray();
	  }

	  public void GuardarEstados(EstadoJuego[] objetos) {
		 string path = "./estados.xml";
		 FileStream outfile = File.Create(path);
		 var formatter = new BinaryFormatter();
		 formatter.Serialize(outfile, objetos);
	  }

	  public EstadoJuego[] CargarEstados() {
		 string path = "./estados.xml";
		 FileStream infile = File.Open(path, FileMode.Open);
		 var formatter = new BinaryFormatter();
		 return (EstadoJuego[])formatter.Deserialize(infile);
	  }

	  public void GuardarAcciones(AccionJuego[] objetos) {
		 string path = "./acciones.xml";
		 FileStream outfile = File.Create(path);
		 var formatter = new BinaryFormatter();
		 formatter.Serialize(outfile, objetos);
	  }

	  public AccionJuego[] CargarAcciones() {
		 string path = "./acciones.xml";
		 FileStream infile = File.Open(path, FileMode.Open);
		 var formatter = new BinaryFormatter();
		 return (AccionJuego[])formatter.Deserialize(infile);
	  }

	  public MDP<EstadoJuego, AccionJuego, TransicionJuego, RecompensaJuego> prepararMDP() {
		 AccionJuego[] acciones = prepararAcciones();
		 //AccionJuego[] acciones = CargarAcciones();
		 EstadoJuego[] estados = prepararEstados(acciones);
		 //EstadoJuego[] estados = CargarEstados();

		 TransicionJuego transicion = new TransicionJuego();
		 RecompensaJuego recompensa = new RecompensaJuego();

		 //GuardarAcciones(acciones);
		 //GuardarEstados(estados);

		 Console.WriteLine("Creando la MDP");
		 MDP<EstadoJuego, AccionJuego, TransicionJuego, RecompensaJuego> mdp = new MDP<EstadoJuego, AccionJuego, TransicionJuego, RecompensaJuego>(estados, acciones, transicion, recompensa, 0.25f);
		 return mdp;
	  }
   }

   class Program {
	  static void Main(string[] args) {
		 TimeSpan inicio = DateTime.Now.TimeOfDay;
		 Resolucion res = new Resolucion(4, 4);
		 MDP<EstadoJuego, AccionJuego, TransicionJuego, RecompensaJuego> mdp = res.prepararMDP();

		 while (true) {
			Juego juego = new Juego(4, 4);
			juego.mdp = mdp;
			juego.estado_actual = mdp.estados[0];
			juego.imprimirTablero();
			while (!juego.GameLoop() && (juego.turno < (4 * 4))) {
			   juego.imprimirTablero();
			}
			juego.imprimirTablero();
			Console.WriteLine("Fin del juego.");
		 }
		 Console.ReadLine();

		 TimeSpan fin = DateTime.Now.TimeOfDay;
		 Console.WriteLine((fin - inicio).TotalMinutes);
		 Console.ReadLine();
	  }
   }
}
