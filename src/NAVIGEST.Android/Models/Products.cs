using System;

namespace NAVIGEST.Android.Models;

public class Product
{
    public string? PRODCODIGO { get; set; }
    public string? PRODNOME { get; set; }
    public string? FAMILIA { get; set; }
    public string? COLABORADOR { get; set; }
    public string? VALOR { get; set; }

    public string? Codigo
    {
        get => PRODCODIGO;
        set => PRODCODIGO = value;
    }

    public string? Descricao
    {
        get => PRODNOME;
        set => PRODNOME = value;
    }

    public string? Familia
    {
        get => FAMILIA;
        set => FAMILIA = value;
    }

    public string? FamiliaSelecionada
    {
        get => FAMILIA;
        set => FAMILIA = value;
    }

    public string? Valor
    {
        get => VALOR;
        set => VALOR = value;
    }

    public string? Colaborador
    {
        get => COLABORADOR;
        set => COLABORADOR = value;
    }

    public string? PrecoCusto
    {
        get => VALOR;
        set => VALOR = value;
    }

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Descricao))
                return "?";

            var parts = Descricao.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpperInvariant();
        }
    }

    public string AvatarColor
    {
        get
        {
            var colors = new[]
            {
                "#FF6B6B", "#4ECDC4", "#45B7D1", "#FFA07A", "#98D8C8",
                "#F7B731", "#5F27CD", "#00D2D3", "#FF9FF3", "#54A0FF",
                "#48DBFB", "#1DD1A1", "#FF6348", "#FF4757", "#2E86DE"
            };

            if (string.IsNullOrWhiteSpace(Descricao))
                return colors[0];

            var index = Math.Abs(char.ToUpperInvariant(Descricao[0]) - 'A') % colors.Length;
            return colors[index];
        }
    }

    public string StatusIcon => "\uf105";
    public string StatusColor => "#C7C7CC";

    public string ValorDisplay => string.IsNullOrWhiteSpace(Valor) ? "0,00€" : Valor;

    public Product Clone() => new()
    {
        PRODCODIGO = PRODCODIGO,
        PRODNOME = PRODNOME,
        FAMILIA = FAMILIA,
        COLABORADOR = COLABORADOR,
        VALOR = VALOR
    };
}
