# Journal App

Egyszerű jegyzetelő asztali alkalmazás.

### Adatbázis

```bash
docker run --name Projekt -e MYSQL_ROOT_PASSWORD=jelszo -e MYSQL_DATABASE=database -p 3306:3306 -d mysql:latest
```

Az `API` mappában:

```bash
dotnet ef database update
```
### Használat

| Művelet | Hogyan |
| --- | --- |
| Új mappa | A bal felső **+** gomb |
| Mappa átnevezése, törlése, új jegyzet hozzáadása | Jobb gombos kattintás a mappán (törlés előtt megerősítést kér) |
| Jegyzet átnevezése, törlése | Jobb gombos kattintás a jegyzeten (törlés előtt megerősítést kér) |
| Jegyzet megnyitása | Kattintás a jegyzetre a bal oldali listában |
| Jegyzet szerkesztése | Írás a jobb oldali mezőben (az **Enter** új sort ír) |
| Átnevezés mentése / elvetése | Az **Enter** vagy a mezőből való kilépés menti, az **Esc** elveti (a név 1–50 karakter lehet) |
| Mappa összecsukása, kinyitása | A mappa neve melletti nyílra kattintva |
| Fájl csatolása a jegyzethez | A megnyitott jegyzetnél a jobb oldali **Csatolmányok** panel **＋ Fájl csatolása** gombja (a fájlnév legfeljebb 50 karakter) |
| Csatolmány letöltése, törlése | A **Csatolmányok** lista adott sorának **Letöltés** / **Törlés** gombja (törlés előtt megerősítést kér) |
| Mentés | A megnyitott jegyzetek módosításai az alkalmazás bezárásakor mentődnek; ha ez nem sikerül, az alkalmazás rákérdez, hogy kilépsz-e mentés nélkül |

### API végpontok

| Metódus | Útvonal | Leírás |
| --- | --- | --- |
| `GET` | `/api/folders` | Az összes mappa a jegyzeteivel |
| `POST` | `/api/folders` | Új mappa (`{ "folderName": "..." }`) |
| `PUT` | `/api/folders/{id}` | Mappa átnevezése |
| `DELETE` | `/api/folders/{folderId}` | Mappa törlése a jegyzeteivel együtt |
| `GET` | `/api/folders/count` | A mappák száma |
| `GET` | `/api/notes` | Az összes jegyzet a csatolmányok adataival (a fájlok tartalma nélkül) |
| `POST` | `/api/notes` | Új jegyzet (`title`, `content`, `folderId`) |
| `PUT` | `/api/notes/{id}` | Jegyzet frissítése (cím, tartalom, mappa) |
| `DELETE` | `/api/notes/{id}` | Jegyzet törlése |
| `GET` | `/api/notes/count` | A jegyzetek száma |
| `GET` | `/api/notes/{id}/count` | Egy jegyzet csatolmányainak száma |
| `GET` | `/api/attachments` | Az összes csatolmány adatai (a fájl tartalma nélkül) |
| `GET` | `/api/attachments/{id}` | Csatolmány letöltése |
| `POST` | `/api/attachments/{noteId}` | Csatolmány feltöltése (`multipart/form-data`, a fájl mezője: `file`) |
| `DELETE` | `/api/attachments/{id}` | Csatolmány törlése |
| `GET` | `/api/attachments/count` | A csatolmányok száma |