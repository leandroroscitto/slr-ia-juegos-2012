using System;
using System.Collections.Generic;

namespace PruebasMarkov2 {
   public enum TAccion {
	  MOVIMIENTO
   }
   public enum TDireccion {
	  U = '8', L = '4', R = '6', D = '2', UL = '7', UR = '9', DL = '1', DR = '3', C = '5'
   }

   public class Accion : Accion_MDP {
	  public static Dictionary<TDireccion, TDireccion> direccion_complementaria;

	  public Juego.Jugador jugador;
	  public TAccion tipo;
	  public TDireccion direccion;
	  public Accion complementaria;

	  public Accion(int i, Juego.Jugador j, TAccion t, TDireccion d) {
		 jugador = j;
		 actor_id = j.id;
		 id = i;
		 tipo = t;
		 direccion = d;
	  }

	  public override bool Equals(object obj) {
		 Accion accion = (Accion)obj;
		 return ((accion.jugador.id == jugador.id) && (accion.tipo == tipo) && (accion.direccion == direccion));
	  }

	  public override int GetHashCode() {
		 return id;
	  }

	  public override string ToString() {
		 return "Accion_id: " + id + ", jugador: " + jugador.nombre + ", tipo: " + tipo + ", direccion: " + direccion;
	  }
   }
}
