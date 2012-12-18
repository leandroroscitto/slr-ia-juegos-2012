using System;
using System.Collections.Generic;

namespace PruebasMarkov2 {
   public class Estado {
	  public int id;
	  public Juego.Zona[][] escenario_base;
	  // <id_jugador,posicion_jugador>
	  public Dictionary<int, Vector2> posicion_jugadores;
	  // <id_objetivo>
	  public HashSet<int> objetivos_cumplidos;
	  // <id_objetivo>
	  public HashSet<int> objetivos_no_cumplidos;

	  public Estado(int i, ref Juego.Zona[][] eb) {
		 id = i;
		 escenario_base = eb;
		 posicion_jugadores = new Dictionary<int, Vector2>();
		 objetivos_cumplidos = new HashSet<int>();
		 objetivos_no_cumplidos = new HashSet<int>();
	  }

	  public bool IntentarAccion(Juego.Jugador jugador, Accion accion, out Vector2 nueva_posicion) {
		 if (accion.tipo == TAccion.MOVIMIENTO) {
			Vector2 posicion_deseada = jugador.posicion;
			switch (accion.direccion) {
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
			   default:
				  break;
			}
			nueva_posicion = posicion_deseada;
			if (escenario_base[posicion_deseada.y][posicion_deseada.x].movilidad == Juego.Zona.TMovilidad.PASABLE) {
			   foreach (int otro_jugador_id in posicion_jugadores.Keys) {
				  if ((otro_jugador_id != jugador.id) && (posicion_jugadores[otro_jugador_id].Equals(posicion_deseada)))
					 return false;
			   }
			   jugador.posicion = posicion_deseada;
			   return true;
			}
			else {
			   return false;
			}
		 }
		 else {
			nueva_posicion = new Vector2();
			return false;
		 }
	  }

	  // Supone que los estados son del mismo juego (mismos jugadores).
	  public override bool Equals(object obj) {
		 Estado estado = (Estado)obj;

		 // Verifica que la posicion de todos los jugadores sea la misma.
		 foreach (int id_jugador in posicion_jugadores.Keys) {
			if (!estado.posicion_jugadores[id_jugador].Equals(posicion_jugadores[id_jugador]))
			   return false;
		 }

		 // Verifica que los objetivos cumplidos sean los mismos.
		 foreach (int id_objetivo in objetivos_cumplidos) {
			if (!estado.objetivos_cumplidos.Contains(id_objetivo))
			   return false;
		 }

		 // Verifica que los objetivos no cumplidos sean los mismos.
		 foreach (int id_objetivo in objetivos_no_cumplidos) {
			if (!estado.objetivos_no_cumplidos.Contains(id_objetivo))
			   return false;
		 }

		 return true;
	  }

	  public override int GetHashCode() {
		 return id;
	  }

	  public override string ToString() {
		 return "Estado_id: " + id + ", objetivos_cumplidos: " + objetivos_cumplidos.Count;
	  }
   }
}
