internal static class ChessSpeech
{
    private static readonly System.Collections.Generic.Dictionary<char, string> Pieces =
        new System.Collections.Generic.Dictionary<char, string> {
            {'K', "king"}, {'Q', "queen"}, {'R', "rook"}, {'B', "bishop"}, {'N', "knight"}
        };
    private static readonly string[] Ranks = {"one","two","three","four","five","six","seven","eight"};

    // Match complete SAN tokens. Piece letters are uppercase and files are lowercase.
    private static readonly System.Text.RegularExpressions.Regex Move =
        new System.Text.RegularExpressions.Regex(
            @"\A(?:(?<castle>O-O-O|O-O|0-0-0|0-0)|" +
            @"(?<piece>[KQRBN])(?<origin>[a-h][1-8]|[a-h]|[1-8])?(?<capture>x)?(?<to>[a-h][1-8])|" +
            @"(?:(?<pawnFile>[a-h])(?<capture>x))?(?<to>[a-h][1-8])(?:=(?<promotion>[QRBN]))?)" +
            @"(?<check>[+#])?[!?]{0,2}\z");

    private static string Square(string square)
    {
        return char.ToUpperInvariant(square[0]) + " " + Ranks[square[1]-'1'];
    }

    private static string Origin(string origin)
    {
        if (origin.Length == 2) return " from " + Square(origin);
        if (origin.Length == 0) return "";
        if (char.IsDigit(origin[0])) return " from rank " + Ranks[origin[0]-'1'];
        return " from the " + char.ToUpperInvariant(origin[0]) + " file";
    }

    private static string ReadMove(string token)
    {
        var m = Move.Match(token);
        if (!m.Success) return null;
        var g = m.Groups;
        string words;
        if (g["castle"].Success) {
            words = g["castle"].Length == 3 ? "castle kingside" : "castle queenside";
        } else {
            string to = g["to"].Value;
            if (g["piece"].Success) {
                words = Pieces[g["piece"].Value[0]] + Origin(g["origin"].Value);
            } else {
                bool backRank = to[1] == '1' || to[1] == '8';
                if (backRank != g["promotion"].Success) return null;
                if (g["pawnFile"].Success && Math.Abs(g["pawnFile"].Value[0] - to[0]) != 1) return null;
                words = "pawn" + Origin(g["pawnFile"].Value);
            }
            words += (g["capture"].Success ? " takes on " : " to ") + Square(to);
            if (g["promotion"].Success) words += ", promotes to " + Pieces[g["promotion"].Value[0]];
        }
        if (g["check"].Value == "+") words += ", check";
        if (g["check"].Value == "#") words += ", checkmate";
        return words;
    }

    public static string Translate(string text)
    {
        var tokens = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"\s+");
        var moves = new System.Collections.Generic.List<string>();
        bool awaitingMove = false;
        foreach (string token in tokens) {
            string move = System.Text.RegularExpressions.Regex.Replace(token, @"\A[1-9][0-9]*\.(?:\.\.)?", "");
            if (move.Length != token.Length && awaitingMove) return text;
            awaitingMove = move.Length == 0;
            if (awaitingMove) continue;
            string spoken = ReadMove(move);
            if (spoken == null) return text;
            moves.Add(spoken);
        }
        return awaitingMove || moves.Count == 0 ? text : string.Join(". ", moves.ToArray());
    }
}
