using System;
using System.Collections.Generic;
using Zona = PruebasMarkov2.Juego.Zona;
using libtcod;

namespace PruebasMarkov2 {
   class CreadorHabitaciones : ITCODBspCallback {
	  public Zona[][] zonas;
	  public float prob_habitacion;
	  public List<TCODBsp> habitaciones;
	  public List<List<TCODBsp>> conexiones;
	  public int borde;

	  public CreadorHabitaciones(ref Zona[][] zons, ref List<TCODBsp> habs, ref List<List<TCODBsp>> con, float prob, int b) {
		 zonas = zons;
		 habitaciones = habs;
		 conexiones = con;
		 prob_habitacion = prob;
		 borde = b;
	  }

	  public TCODBsp GetHojaI(TCODBsp node) {
		 while (!node.isLeaf()) {
			if (node.getLeft() != null)
			   node = node.getLeft();
			else if (node.getRight() != null)
			   node = node.getRight();
		 }
		 return node;
	  }

	  public TCODBsp GetHojaD(TCODBsp node) {
		 while (!node.isLeaf()) {
			if (node.getRight() != null)
			   node = node.getRight();
			else if (node.getLeft() != null)
			   node = node.getLeft();
		 }
		 return node;
	  }


	  void vline(int x, int y1, int y2) {
		 int y = y1;
		 int dy = (y1 > y2 ? -1 : 1);
		 zonas[x][y].tipo = Zona.TZona.PISO;
		 zonas[x][y].representacion = (char)Zona.TZona.PISO;
		 zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
		 if (y1 == y2) return;
		 do {
			y += dy;
			zonas[x][y].tipo = Zona.TZona.PISO;
			zonas[x][y].representacion = (char)Zona.TZona.PISO;
			zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
		 } while (y != y2);
	  }

	  void vline_up(int x, int y) {
		 while (y >= 0 && zonas[x][y].movilidad != Zona.TMovilidad.PASABLE) {
			zonas[x][y].tipo = Zona.TZona.PISO;
			zonas[x][y].representacion = (char)Zona.TZona.PISO;
			zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
			y--;
		 }
	  }

	  void vline_down(int x, int y) {
		 while (y < zonas[x].Length && zonas[x][y].movilidad != Zona.TMovilidad.PASABLE) {
			zonas[x][y].tipo = Zona.TZona.PISO;
			zonas[x][y].representacion = (char)Zona.TZona.PISO;
			zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
			y++;
		 }
	  }

	  void hline(int x1, int y, int x2) {
		 int x = x1;
		 int dx = (x1 > x2 ? -1 : 1);
		 zonas[x][y].tipo = Zona.TZona.PISO;
		 zonas[x][y].representacion = (char)Zona.TZona.PISO;
		 zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
		 if (x1 == x2) return;
		 do {
			x += dx;
			zonas[x][y].tipo = Zona.TZona.PISO;
			zonas[x][y].representacion = (char)Zona.TZona.PISO;
			zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
		 } while (x != x2);
	  }

	  void hline_left(int x, int y) {
		 while (x >= 0 && zonas[x][y].movilidad != Zona.TMovilidad.PASABLE) {
			zonas[x][y].tipo = Zona.TZona.PISO;
			zonas[x][y].representacion = (char)Zona.TZona.PISO;
			zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
			x--;
		 }
	  }

	  void hline_right(int x, int y) {
		 while (x < zonas.Length && zonas[x][y].movilidad != Zona.TMovilidad.PASABLE) {
			zonas[x][y].tipo = Zona.TZona.PISO;
			zonas[x][y].representacion = (char)Zona.TZona.PISO;
			zonas[x][y].movilidad = Zona.TMovilidad.PASABLE;
			x++;
		 }
	  }

	  public void CrearCamino(TCODBsp nodo1, TCODBsp nodo2, bool horizontal) {
		 int x1, y1, x2, y2, w1, h1, w2, h2;
		 x1 = nodo1.x;
		 y1 = nodo1.y;
		 w1 = nodo1.w - 2;
		 h1 = nodo1.h - 2;
		 x2 = nodo2.x;
		 y2 = nodo2.y;
		 w2 = nodo2.w - 2;
		 h2 = nodo2.h - 2;

		 if (horizontal) {
			if ((x1 + w1 - 1 < x2) || (x2 + w2 - 1 < x1)) {
			   int xo = TCODRandom.getInstance().getInt(x1, x1 + w1 - 1);
			   int xd = TCODRandom.getInstance().getInt(x2, x2 + w2 - 1);
			   int yo = TCODRandom.getInstance().getInt(y1 + h1, y2);
			   vline_up(xo, yo - 1);
			   hline(xo, yo, xd);
			   vline_down(xd, yo + 1);
			}
			else {
			   int minx = Math.Max(x1, x2);
			   int maxx = Math.Min(x1 + w1 - 1, x2 + w2 - 1);
			   int xo = TCODRandom.getInstance().getInt(minx, maxx);
			   vline_down(xo, y2);
			   vline_up(xo, y2 - 1);
			}
		 }
		 else {
			if ((y1 + h1 - 1 < y2) || (y2 + h2 - 1 < y1)) {
			   int yo = TCODRandom.getInstance().getInt(y1, y1 + h1 - 1);
			   int yd = TCODRandom.getInstance().getInt(y2, y2 + h2 - 1);
			   int xo = TCODRandom.getInstance().getInt(x1 + w1, x2);
			   hline_left(xo - 1, yo);
			   hline(xo, yo, yd);
			   hline_right(xo + 1, yd);
			}
			else {
			   int miny = Math.Max(y1, y2);
			   int maxy = Math.Min(y1 + h1 - 1, y2 + y2 - 1);
			   int yo = TCODRandom.getInstance().getInt(miny, maxy);
			   hline_left(x2 - 1, yo);
			   hline_right(x2, yo);
			}
		 }
	  }

	  public override bool visitNode(TCODBsp node) {
		 if (node.isLeaf()) {
			if (TCODRandom.getInstance().getGaussianRangeFloat(0, 1) < prob_habitacion) {
			   for (int i = node.x + borde; i < (node.x + node.w) - (1 + borde); i++) {
				  for (int j = node.y + borde; j < (node.y + node.h) - (1 + borde); j++) {
					 zonas[i][j].tipo = Zona.TZona.PISO;
					 zonas[i][j].representacion = (char)Zona.TZona.PISO;
					 zonas[i][j].movilidad = Zona.TMovilidad.PASABLE;
				  }
			   }
			   habitaciones.Add(node);
			   List<TCODBsp> conj = new List<TCODBsp>();
			   conj.Add(node);
			   conexiones.Add(conj);
			}
		 }
		 else {
			TCODBsp hijo_izq, hijo_der;
			hijo_izq = node.getLeft();
			hijo_der = node.getRight();

			node.x = Math.Min(hijo_izq.x, hijo_der.x);
			node.y = Math.Min(hijo_izq.y, hijo_der.y);
			node.w = Math.Max(hijo_izq.x + hijo_izq.w, hijo_der.x + hijo_der.w) - node.x;
			node.h = Math.Max(hijo_izq.y + hijo_izq.h, hijo_der.y + hijo_der.h) - node.y;

			if ((hijo_izq != null) && (hijo_der != null)) {
			   List<TCODBsp> conji, conjd;
			   conji = conjd = null;
			   foreach (List<TCODBsp> subc in conexiones) {
				  foreach (TCODBsp nodo in subc) {
					 if (nodo.contains(hijo_izq.x, hijo_izq.y) && hijo_izq.contains(nodo.x, nodo.y))
						conji = subc;
					 if (nodo.contains(hijo_der.x, hijo_der.y) && hijo_der.contains(nodo.x, nodo.y))
						conjd = subc;
				  }
			   }
			   if ((conji != null && conjd != null) && conji != conjd) {
				  conji.AddRange(conjd);
				  conexiones.Remove(conjd);

				  CrearCamino(hijo_izq, hijo_der, node.horizontal);
			   }
			}
		 }
		 return true;
	  }
   }

   class Generador_Habitaciones {
	  public static Zona[][] GenerarHabitaciones(int ancho, int alto,ref Juego.Objetivo[] objs) {
		 Zona[][] zonas = new Zona[ancho][];
		 for (int i = 0; i < ancho; i++) {
			zonas[i] = new Zona[alto];
			for (int j = 0; j < alto; j++) {
			   zonas[i][j] = new Zona(Zona.TZona.PARED, Zona.TMovilidad.IMPASABLE);
			   zonas[i][j].representacion = (char)Zona.TZona.PARED;
			}
		 }
		 TCODBsp bsp = new TCODBsp(1, 1, ancho - 1, alto - 1);
		 bsp.splitRecursive(TCODRandom.getInstance(), (int)Math.Sqrt(ancho * alto), Math.Min(6, Math.Max((int)Math.Sqrt(ancho), 2)), Math.Min(6, Math.Max((int)Math.Sqrt(alto), 2)), 1.6f, 1.6f);

		 List<TCODBsp> habitaciones = new List<TCODBsp>();
		 List<List<TCODBsp>> conexiones = new List<List<TCODBsp>>();
		 CreadorHabitaciones creador_habitaciones = new CreadorHabitaciones(ref zonas, ref habitaciones, ref conexiones, 0.95f, 0);
		 bsp.traverseInvertedLevelOrder(creador_habitaciones);

		 if (habitaciones.Count > 0)
			foreach (Juego.Objetivo objetivo in objs) {
			   TCODBsp habitacion = habitaciones[TCODRandom.getInstance().getGaussianRangeInt(0, habitaciones.Count - 1)];
			   Vector2 posicion_objetivo = new Vector2(habitacion.x + TCODRandom.getInstance().getGaussianRangeInt(0, habitacion.w - 2), habitacion.y + TCODRandom.getInstance().getGaussianRangeInt(0, habitacion.h - 2));

			   if (habitaciones.Count > 1)
				  habitaciones.Remove(habitacion);

			   if (posicion_objetivo.y >= alto - 1)
				  posicion_objetivo.y--;

			   if (posicion_objetivo.x >= ancho - 1)
				  posicion_objetivo.x--;

			   zonas[posicion_objetivo.y][posicion_objetivo.x] = objetivo;
			   objetivo.posicion = posicion_objetivo;
			}

		 return zonas;
	  }
   }
}
