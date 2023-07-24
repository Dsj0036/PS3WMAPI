namespace System
{/// <summary>
 /// Representa una pareja de valores de tipo T.
 /// </summary>
 /// <typeparam name="T">El tipo de los valores de la pareja.</typeparam>
    public class Pair<T> where T : struct
    {
        /// <summary>
        /// Obtiene el valor del primer elemento de la pareja.
        /// </summary>
        public T X { get; private set; }

        /// <summary>
        /// Obtiene el valor del segundo elemento de la pareja.
        /// </summary>
        public T Y { get; private set; }

        /// <summary>
        /// Crea una nueva instancia de la clase Pair con los valores de los dos elementos especificados.
        /// </summary>
        /// <param name="x">El valor del primer elemento de la pareja.</param>
        /// <param name="y">El valor del segundo elemento de la pareja.</param>
        public Pair(T x, T y)
        {
            this.X = x;
            this.Y = y;
        }
        public void SetX(T x) => this.X = x;
        public void SetY(T y) => this.Y = y;
        /// <summary>
        /// Crea una nueva instancia de la clase Pair con los valores predeterminados de los dos elementos.
        /// </summary>
        private Pair() { }

        /// <summary>
        /// Devuelve una cadena que representa la pareja en formato de cadena.
        /// </summary>
        /// <returns>Una cadena que representa la pareja en formato de cadena.</returns>
        public override string ToString() => $"X {{{X}}} | Y {{{Y}}}";
    }
}