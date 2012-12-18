using System;
using System.Collections.Generic;
using Zona = PruebasMarkov2.Juego.Zona;
using libtcod;

namespace PruebasMarkov2 {
   class Generador_Escenario {

	  class BspListner : ITCODBspCallback {
		 public Zona[][] zonas;
		 public List<Habitacion> habitaciones;
		 private float prob_aceptar;

		 public BspListner(ref Zona[][] zs, ref List<Habitacion> habs, float pa) {
			zonas = zs;
			prob_aceptar = pa;
			habitaciones = habs;
		 }

		 public override bool visitNode(TCODBsp node) {
			int offset = 2;
			if (node.isLeaf() && TCODRandom.getInstance().getGaussianRangeFloat(0, 1) < prob_aceptar) {
			   for (int i = node.y + offset; i < node.y + node.h - offset; i++) {
				  for (int j = node.x + offset; j < node.x + node.w - offset; j++) {
					 zonas[i][j].tipo = Zona.TZona.PISO;
					 zonas[i][j].representacion = (char)Zona.TZona.PISO;
					 zonas[i][j].movilidad = Zona.TMovilidad.PASABLE;
				  }
			   }

			   if ((node.w > 2 * offset) && (node.h > 2 * offset))
				  habitaciones.Add(new Habitacion(habitaciones.Count, new Vector2(node.x + offset, node.y + offset), new Vector2(node.w - offset, node.h - offset)));
			}

			if (!node.isLeaf()) {
			   TCODBsp node1, node2;
			   Vector2 p1, p2, p3, p4;

			   node1 = node.getLeft();
			   node2 = node.getRight();

			   p1 = new Vector2(node1.x + node1.w / 2, node1.y + node1.h / 2);
			   p2 = new Vector2((node1.x + node2.x + node1.w) / 2, node1.y + node1.h / 2);
			   p3 = new Vector2((node1.x + node2.x + node1.w) / 2, node2.y + node2.h / 2);
			   p4 = new Vector2(node2.x + node2.w / 2, node2.y + node2.h / 2);

			   TCODLine.init(p1.x, p1.y, p2.x, p2.y);
			   int i = 0;
			   int j = 0;
			   while (!TCODLine.step(ref i, ref j) && (j < zonas.Length) && (i < zonas[0].Length)) {
				  zonas[j][i].tipo = Zona.TZona.PISO;
				  if (zonas[j][i].representacion == (char)TCODSpecialCharacter.VertLine)
					 zonas[j][i].representacion = (char)TCODSpecialCharacter.Cross;
				  else
					 zonas[j][i].representacion = (char)TCODSpecialCharacter.HorzLine;
				  zonas[j][i].movilidad = Zona.TMovilidad.PASABLE;
			   }

			   TCODLine.init(p2.x, p2.y, p3.x, p3.y);
			   i = 0;
			   j = 0;
			   while (!TCODLine.step(ref i, ref j) && (j < zonas.Length) && (i < zonas[0].Length)) {
				  zonas[j][i].tipo = Zona.TZona.PISO;
				  if (zonas[j][i].representacion == (char)TCODSpecialCharacter.HorzLine)
					 zonas[j][i].representacion = (char)TCODSpecialCharacter.Cross;
				  else
					 zonas[j][i].representacion = (char)TCODSpecialCharacter.VertLine;
				  zonas[j][i].movilidad = Zona.TMovilidad.PASABLE;
			   }

			   TCODLine.init(p3.x, p3.y, p4.x, p4.y);
			   i = 0;
			   j = 0;
			   while (!TCODLine.step(ref i, ref j) && (j < zonas.Length) && (i < zonas[0].Length)) {
				  zonas[j][i].tipo = Zona.TZona.PISO;
				  if (zonas[j][i].representacion == (char)TCODSpecialCharacter.VertLine)
					 zonas[j][i].representacion = (char)TCODSpecialCharacter.Cross;
				  else
					 zonas[j][i].representacion = (char)TCODSpecialCharacter.HorzLine;
				  zonas[j][i].movilidad = Zona.TMovilidad.PASABLE;
			   }
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

		 float[][] nueva_zona_float = new float[al][];
		 for (int i = 0; i < al; i++) {
			nueva_zona_float[i] = new float[an];
			for (int j = 0; j < an; j++) {
			   int valor1 = (zona1[i][j].tipo == Zona.TZona.PISO || zona1[i][j].tipo == Zona.TZona.OBJETIVO) ? 1 : 0;
			   int valor2 = (zona2[i][j].tipo == Zona.TZona.PISO || zona2[i][j].tipo == Zona.TZona.OBJETIVO) ? 1 : 0;
			   float nuevo_valor = 8 * (coef * valor1 + (1.0f - coef) * valor2);
			   nueva_zona_float[i][j] = nuevo_valor;
			}
		 }

		 for (int i = 1; i < al - 1; i++) {
			for (int j = 1; j < an - 1; j++) {
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

	  public static Zona[][] generarEscenario(int an, int al, Juego.Objetivo[] objs) {
		 Zona[][] resultado = new Zona[al][];
		 for (int i = 0; i < al; i++) {
			resultado[i] = new Zona[an];
			for (int j = 0; j < an; j++) {
			   resultado[i][j] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
			}
		 }

		 List<Habitacion> habitaciones = new List<Habitacion>();

		 TCODBsp bsproot = new TCODBsp(0, 0, an, al);
		 bsproot.splitRecursive(null, 8, an / 4, al / 4, 1.0f, 0.75f);
		 BspListner listener = new BspListner(ref resultado, ref habitaciones, 0.85f);
		 bsproot.traverseInvertedLevelOrder(listener);

		 if (habitaciones.Count > 0)
			foreach (Juego.Objetivo objetivo in objs) {
			   Habitacion habitacion = habitaciones[TCODRandom.getInstance().getGaussianRangeInt(0, habitaciones.Count - 1)];
			   Vector2 posicion_objetivo = new Vector2(habitacion.posicion.x + TCODRandom.getInstance().getGaussianRangeInt(0, habitacion.tamano.x - 2), habitacion.posicion.y + TCODRandom.getInstance().getGaussianRangeInt(0, habitacion.tamano.y - 2));

			   if (habitaciones.Count > 1)
				  habitaciones.Remove(habitacion);

			   if (posicion_objetivo.y >= al - 1)
				  posicion_objetivo.y--;

			   if (posicion_objetivo.x >= an - 1)
				  posicion_objetivo.x--;

			   resultado[posicion_objetivo.y][posicion_objetivo.x] = objetivo;
			   objetivo.posicion = posicion_objetivo;
			}

		 return resultado;
	  }

	  public static Zona[][] generarEscenario(int an, int al, Juego.Objetivo[] objs, float prob_paredes) {
		 Zona[][] resultado;
		 resultado = new Zona[al][];

		 int cantidad_paredes = 0;
		 int cantidad_zonas_cubiertas = 0;
		 int cantidad_zonas_usables = (an - 2) * (al - 2);

		 Random R = new Random(DateTime.Now.Millisecond * DateTime.Now.Second * DateTime.Now.Minute);
		 float dado;

		 Dictionary<Juego.Objetivo, Vector2> posicion_objetivos = new Dictionary<Juego.Objetivo, Vector2>();
		 foreach (Juego.Objetivo objetivo in objs) {
			Vector2 posicion_objetivo = new Vector2(R.Next(1, al - 1), R.Next(1, an - 1));
			posicion_objetivos.Add(objetivo, posicion_objetivo);
			objetivo.posicion = posicion_objetivo;
		 }

		 resultado[0] = new Zona[an];
		 for (int j = 0; j < an; j++) {
			resultado[0][j] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
		 }
		 for (int i = 1; i < al - 1; i++) {
			resultado[i] = new Zona[an];
			resultado[i][0] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);

			for (int j = 1; j < an - 1; j++) {
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
			resultado[i][an - 1] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
		 }
		 resultado[al - 1] = new Zona[an];
		 for (int j = 0; j < an; j++) {
			resultado[al - 1][j] = new Zona(Zona.TZona.LIMITE, Zona.TMovilidad.IMPASABLE);
		 }

		 for (int iteracion = 0; iteracion < 4; iteracion++) {
			for (int i = 1; i < al - 1; i++) {
			   for (int j = 1; j < an - 1; j++) {
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
			for (int i = 1; i < al - 1; i++) {
			   for (int j = 1; j < an - 1; j++) {
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
			   if (((i + q) >= 0 && (i + q) < alto) && ((j + k) >= 0 && (j + k) < ancho))
				  if ((zonas[i + q][j + k].tipo == Zona.TZona.PARED) || (zonas[i + q][j + k].tipo == Zona.TZona.LIMITE))
					 cant++;
			}
		 }
		 return cant;
	  }
   }

}
