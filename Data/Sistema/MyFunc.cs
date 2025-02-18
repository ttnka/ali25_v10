using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ali25_V10.Data.Sistema;
public static class MyFunc
{
    public static string DiaTitulo(int dia, int completo = 0)
    {
        string valores = "Dom,Lun,Mar,Mie,Jue,Vie,Sab,Domingo,Lunes,Martes,Miercoles,Jueves,Viernes,Sabado";
        var arr = valores.Split(",");
        completo = completo > 1 ? 0 : completo * 7;

        return arr[(dia - 1 + completo)];
    }
    public static string MesTitulo(int mes, int completo = 0)
    {
        string valores = "Ene,Feb,Mar,Abr,May,Jun,Jul,Ago,Sep,Oct,Nov,Dic,Enero,Febrero,Marzo,Abril,Mayo,Junio,Julio,Agosto,Septiembre,Octubre,Noviembre,Diciembre";
        var arr = valores.Split(",");
        completo = completo > 1 ? 0 : completo * 12;

        return arr[(mes - 1 + completo)];
    }
    public static int Ejercicio(DateTime fecha)
    {
        int year = fecha.Year;
        return year < 2000 ? year - 1900 : year - 2000;
    }
    public static string LaHora(DateTime lahora, string formato)
    {
        switch (formato)
        {
            case "M":
                string cero = lahora.Minute < 10 ? "0" : "";
                return $"{lahora.Hour}:{cero}{lahora.Minute}";

            default:
                string mincero = lahora.Minute < 10 ? "0" : "";
                string segcero = lahora.Second < 10 ? "0" : "";
                return $"{lahora.Hour}:{mincero}{lahora.Minute}:{segcero}{lahora.Second}";
        }
    }
    public static string FormatoFecha(string formato, DateTime lafecha)
    {
        string resultado = string.Empty;

        switch (formato)
        {
            case "DD/MMM/AA":
                resultado = $"{lafecha.Day}/";
                resultado += $"{MesTitulo(lafecha.Month, 0)}/";
                resultado += $"{Ejercicio(lafecha)}";
                break;

        }
        return resultado;
    }
    public static string FormatoRFC(string rfc)
    {
        if (rfc == null) return string.Empty;
        int i = rfc.Length == 13 ? 1 : 0;

        return rfc.Substring(0, 3 + i) + "-" +
            rfc.Substring(3 + i, 6) + "-" + rfc.Substring(9 + i, 3);

    }
    public static int DameRandom(int inicio, int final)
    {
        Random rnd = new Random();
        return rnd.Next(inicio, final);
    }

    public static string MyGuid(string tabla) 
    {
        if (tabla.Length > 4)
        tabla = tabla.Substring(0, 4);

        string respuesta = "";
        string[] vGuid = new string[] 
        {"00","01","02","03","04","05","06","07","08","09","a","b","c","d","e","f","g",
        "h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z","A",
        "B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U",
        "V","W","X","Y","Z",};

        DateTime fec = DateTime.Now;
        for (int v = 0; v < 9; v++)
        {
            int valor = 0;
            if(v == 0 || v == 1)
            {
                string val = v == 1 ? fec.Year.ToString().Substring(2,2) : fec.Year.ToString().Substring(0,2);
                valor = int. Parse(val);    
            } 
            else if (v == 3) valor = fec.Month;
            else if (v == 4) valor = fec.Day;
            else if (v == 5) valor = fec.Hour;
            else if (v == 6) valor = fec.Minute;
            else if(v == 7) valor = fec.Second;
            else valor = fec.Millisecond / 10;

            if (valor < 10) 
                {respuesta += vGuid[valor];}
            else if(valor < 62)
                {respuesta += "1" + vGuid[valor];}
            else 
                respuesta += "2" + vGuid[valor - 62 + 10];
        }
        return tabla + "-" + respuesta + "-" + Guid.NewGuid().ToString();
    }

}