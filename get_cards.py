import csv
import json

names = [
    "TESORO GENERICO", "ROJO FUGAZ", "SOMBRA DEL DESIERTO", "HECHICERA ELEMENTAL",
    "ANCIANA MAESTRA", "CUMULO DE HONGOS", "CIUDAD EN LLAMAS", "MUERTE INMINENTE",
    "PLANES FRUSTRADOS", "RITUAL DE NEGACION", "LIDER DE LA MANADA", "FELINO DE LA MONTAÑA",
    "GATITOS DE BRUJA", "CASCABUFALO", "NICOL, LA APRENDIZ"
]

with open(r'c:\Users\Work\Dev\LairenArena\LairenArena\Assets\Lairen - Hoja Piola.xlsx - +Ancestros.csv', encoding='utf-8') as f:
    reader = csv.reader(f)
    header = next(reader)
    found = []
    for row in reader:
        if len(row) > 1 and row[1] in names:
            found.append({
                "Name": row[1],
                "Cost": row[8],
                "Attack": row[11],
                "Defense": row[12],
                "Text": row[7]
            })

with open(r'c:\Users\Work\Dev\LairenArena\LairenArena\tmp_cards.json', 'w', encoding='utf-8') as f:
    json.dump(found, f, indent=2, ensure_ascii=False)
