using System;
using System.Collections.Generic;
using Zona = PruebasMarkov2.Juego.Zona;
using libtcod;

namespace PruebasMarkov2 {
   class Generador_Escenario2 {

	  class BspListner : ITCODBspCallback {
		 public Zona[][] zonas;
		 public List<Habitacion> habitaciones;
		 private float prob_aceptar;
		 private int lastx, lasty;

		 public void dig(int x1, int y1, int x2, int y2) {
			if (x2 < x1) {
			   int tmp = x2;
			   x2 = x1;
			   x1 = tmp;
			}
			if (y2 < y1) {
			   int tmp = y2;
			   y2 = y1;
			   y1 = tmp;
			}
			for (int tilex = x1; tilex <= x2; tilex++) {
			   for (int tiley = y1; tiley <= y2; tiley++) {
				  zonas[tilex][tiley].movilidad = Zona.TMovilidad.PASABLE;
				  zonas[tilex][tiley].representacion = (char)Zona.TZona.PISO;
				  zonas[tilex][tiley].tipo = Zona.TZona.PISO;
			   }
			}
		 }

		 public BspListner(ref Zona[][] zs, ref List<Habitacion> habs, float pa) {
			zonas = zs;
			prob_aceptar = pa;
			habitaciones = habs;
		 }

		 public override bool visitNode(TCODBsp node) {
			if (node.isLeaf() && TCODRandom.getInstance().getGaussianRangeFloat(0, 1) < prob_aceptar) {
			   int x, y, w, h;
			   TCODRandom rng = TCODRandom.getInstance();
			   w = rng.getInt(ROOM_MIN_SIZE, node.w - 2);
			   h = rng.getInt(ROOM_MIN_SIZE, node.h - 2);
			   x = rng.getInt(node.x + 1, node.x + node.w - w - 1);
			   y = rng.getInt(node.y + 1, node.y + node.h - h - 1);
			   habitaciones.Add(new Habitacion(habitaciones.Count, new Vector2(x, y), new Vector2(x + w - 1, y + h - 1)));
			   dig(x, y, x + w - 1, y + h - 1);

			   if (habitaciones.Count != 1) {
				  dig(lastx, lasty, x + w / 2, lasty);
				  dig(x + w / 2, lasty, x + w / 2, y + h / 2);
			   }
			   lastx = x+w/2;
			   lasty = y+h/2;
			}

			return true;
		 }
	  }

	  struct Habitacion {
		 public int id;
		 public Vector2 posicion;
		 public Vector2 tamano;

		 public Habitacion(int i, Vector2 p, Vector2 t) {
			id = i;
			posicion = p;
			tamano = t;
		 }
	  }

	  public static Zona[][] generarEscenario(int an, int al, Juego.Objetivo[] objs, float probp, float coef) {
		 Zona[][] zona1 = generarEscenario(an, al, objs);
		 Zona[][] zona2 = generarEscenario(an, al, objs, probp);

		 float[][] nueva_zona_float = new float[an][];
		 for (int i = 0; i < an; i++) {
			nueva_zona_float[i] = new float[al];
			for (int j = 0; j < al; j++) {
			   int valor1 = (zona1[i][j].tipo == Zona.TZona.PISO || zona1[i][j].tipo == Zona.TZona.OBJETIVO) ? 1 : 0;
			   int valor2 = (zona2[i][j].tipo == Zona.TZona.PISO || zona2[i][j].tipo == Zona.TZona.OBJETIVO) ? 1 : 0;
			   float nuevo_valor = 8 * (coef * valor1 + (1.0f - coef) * valor2);
			   nueva_zona_float[i][j] = nuevo_valor;
			}
		 }

		 for (int i = 1; i < an - 1; i++) {
			for (int j = 1; j < al - 1; j++) {
			   float region_zona = SumaAlrededor(ref nueva_zona_float, i, j);
			   bool piso = (nueva_zona_float[i][j] * 5 + region_zona / 4) >= 40;
			   if (piso && (zona1[i][j].tipo == Zona.TZona.PARED || zona1[i][j].tipo == Zona.TZona.LIMITE)) {
				  zona2[i][j].tipo = Zona.TZona.PISO;
				  zona2[i][j].movilidad = Zona.TMovilidad.PASABLE;
				  zona2[i][j].representacion = '4';
			   }
			   if (!piso && zona1[i][j].tipo == Zona.TZona.PISO) {
				  zona2[i][j].tipo = Zona.TZona.PARED;
				  zona2[i][j].movilidad = Zona.TMovilidad.IMPASABLE;
				  zona2[i][j].representacion = '#';
			   }
			}
		 }

		 return zona1;
	  }

	  public static float SumaAlrededor(ref float[][] zona, int x, int y) {
		 float suma = 0;
		 for (int i = -1; i < 1; i++) {
			for (int j = -1; j < 1; j++) {
			   suma += zona[x + i][y + j];
			}
		 }
		 suma -= zona[x][y];
		 return suma;
	  }

	  public static int ROOM_MAX_SIZE = 4;
	  public static int ROOM_MIN_SIZE = 2;

	  public static Zona[][] generarEscenario(int an, int al, Juego.Objetivo[] objs) {
		 Zona[][] resultado = new Zona[an][];
		 for (int i = 0; i < an; i++) {
			resultado[i] = new Zona[al];
			for (int j = 0; j < al; j++) {
			   resultado[i][j] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
			}
		 }

		 List<Habitacion> habitaciones = new List<Habitacion>();

		 TCODBsp bsproot = new TCODBsp(0, 0, an, al);
		 bsproot.splitRecursive(null, 12, ROOM_MAX_SIZE, ROOM_MAX_SIZE, 1.5f, 1.5f);
		 BspListner listener = new BspListner(ref resultado, ref habitaciones, 1.0f);
		 bsproot.traverseInvertedLevelOrder(listener);

		 if (habitaciones.Count > 0)
			foreach (Juego.Objetivo objetivo in objs) {
			   Habitacion habitacion = habitaciones[TCODRandom.getInstance().getGaussianRangeInt(0, habitaciones.Count - 1)];
			   Vector2 posicion_objetivo = new Vector2(habitacion.posicion.x + TCODRandom.getInstance().getGaussianRangeInt(0, habitacion.tamano.x), habitacion.posicion.y + TCODRandom.getInstance().getGaussianRangeInt(0, habitacion.tamano.y));

			   if (habitaciones.Count > 1)
				  habitaciones.Remove(habitacion);

			   while (posicion_objetivo.y >= al - 1)
				  posicion_objetivo.y--;

			   while (posicion_objetivo.x >= an - 1)
				  posicion_objetivo.x--;

			   resultado[posicion_objetivo.x][posicion_objetivo.y] = objetivo;
			   objetivo.posicion = posicion_objetivo;
			}

		 return resultado;
	  }

	  public static Zona[][] generarEscenario(int an, int al, Juego.Objetivo[] objs, float prob_paredes) {
		 Zona[][] resultado;
		 resultado = new Zona[an][];

		 int cantidad_paredes = 0;
		 int cantidad_zonas_cubiertas = 0;
		 int cantidad_zonas_usables = (an - 2) * (al - 2);

		 Random R = new Random(DateTime.Now.Millisecond * DateTime.Now.Second * DateTime.Now.Minute);
		 float dado;

		 Dictionary<Juego.Objetivo, Vector2> posicion_objetivos = new Dictionary<Juego.Objetivo, Vector2>();
		 foreach (Juego.Objetivo objetivo in objs) {
			Vector2 posicion_objetivo = new Vector2(R.Next(1, an - 1), R.Next(1, al - 1));
			posicion_objetivos.Add(objetivo, posicion_objetivo);
			objetivo.posicion = posicion_objetivo;
		 }

		 resultado[0] = new Zona[al];
		 for (int j = 0; j < al; j++) {
			resultado[0][j] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
		 }
		 for (int i = 1; i < an - 1; i++) {
			resultado[i] = new Zona[al];
			resultado[i][0] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);

			for (int j = 1; j < al - 1; j++) {
			   bool objetivo_posicionado = false;
			   foreach (Juego.Objetivo objetivo in posicion_objetivos.Keys) {
				  Vector2 posicion = posicion_objetivos[objetivo];
				  if (posicion.x == i && posicion.y == j) {
					 resultado[i][j] = objetivo;
					 objetivo_posicionado = true;
					 break;
				  }
			   }

			   if (!objetivo_posicionado) {
				  float razon_cubierta_paredes = cantidad_paredes * 1f / (cantidad_zonas_cubiertas + 1);
				  dado = R.Next(0, 100) / 100f;
				  if (dado < (prob_paredes)) {
					 resultado[i][j] = new Zona(Zona.TZona.PARED, Zona.TMovilidad.IMPASABLE);
					 cantidad_paredes++;
				  }
				  else {
					 resultado[i][j] = new Zona(Zona.TZona.PISO, Zona.TMovilidad.PASABLE);
				  }
			   }
			   cantidad_zonas_cubiertas++;
			}
			resultado[i][al - 1] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
		 }
		 resultado[an - 1] = new Zona[al];
		 for (int j = 0; j < al; j++) {
			resultado[an - 1][j] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
		 }

		 for (int iteracion = 0; iteracion < 4; iteracion++) {
			for (int i = 1; i < an - 1; i++) {
			   for (int j = 1; j < al - 1; j++) {
				  int cant1 = ParedesN(ref resultado, i, j, an, al, 1);
				  int cantn = ParedesN(ref resultado, i, j, an, al, 2);
				  if ((cant1 >= 5) || (cantn <= 2)) {
					 resultado[i][j].tipo = Zona.TZona.PARED;
					 resultado[i][j].representacion = (char)248;
					 resultado[i][j].movilidad = Zona.TMovilidad.IMPASABLE;
				  }
				  else {
					 resultado[i][j].tipo = Zona.TZona.PISO;
					 resultado[i][j].representacion = 'O';
					 resultado[i][j].movilidad = Zona.TMovilidad.PASABLE;
				  }
			   }
			}
		 }

		 for (int iteracion = 0; iteracion < 0; iteracion++) {
			for (int i = 1; i < an - 1; i++) {
			   for (int j = 1; j < al - 1; j++) {
				  int cant1 = ParedesN(ref resultado, i, j, an, al, 1);
				  if (cant1 >= 5) {
					 resultado[i][j].tipo = Zona.TZona.PARED;
					 resultado[i][j].representacion = (char)248;
					 resultado[i][j].movilidad = Zona.TMovilidad.IMPASABLE;
				  }
				  else {
					 resultado[i][j].tipo = Zona.TZona.PISO;
					 resultado[i][j].representacion = 'O';
					 resultado[i][j].movilidad = Zona.TMovilidad.PASABLE;
				  }
			   }
			}
		 }

		 return resultado;
	  }

	  public static int ParedesN(ref Juego.Zona[][] zonas, int i, int j, int ancho, int alto, int n) {
		 int cant = 0;
		 for (int q = -1 * n; q <= 1 * n; q++) {
			for (int k = -1 * n; k <= 1 * n; k++) {
			   if (((i + q) >= 0 && (i + q) < ancho) && ((j + k) >= 0 && (j + k) < alto))
				  if ((zonas[i + q][j + k].tipo == Zona.TZona.PARED) || (zonas[i + q][j + k].tipo == Zona.TZona.LIMITE))
					 cant++;
			}
		 }
		 return cant;
	  }
   }

}
