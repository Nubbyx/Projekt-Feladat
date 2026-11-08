# Journal App

Egyszerű jegyzetelő asztali alkalmazás.

### Adatbázis

```bash
docker run --name Projekt -e MYSQL_ROOT_PASSWORD=jelszo -e MYSQL_DATABASE=database -p 3306:3306 -d mysql:latest
```

### Használat

| Művelet | Hogyan |
| --- | --- |
| Új mappa | A bal felső **+** gomb |
| Mappa átnevezése, törlése, új jegyzet hozzáadása | Jobb gombos kattintás a mappán |
| Jegyzet átnevezése, törlése | Jobb gombos kattintás a jegyzeten |
| Jegyzet megnyitása | Kattintás a jegyzetre a bal oldali listában |
| Jegyzet szerkesztése | Írás a jobb oldali mezőben (az **Enter** új sort ír) |

### API végpontok

| Metódus | Útvonal | Leírás |
| --- | --- | --- |
| `GET` | `/api/folders` | Az összes mappa |
| `POST` | `/api/folders` | Új mappa (`{ "folderName": "..." }`) |
| `PUT` | `/api/folders/{id}` | Mappa átnevezése |
| `DELETE` | `/api/folders/{folderId}` | Mappa törlése a jegyzeteivel együtt |
| `GET` | `/api/notes` | Az összes jegyzet a csatolmányaikkal |
| `POST` | `/api/notes` | Új jegyzet (`title`, `content`, `folderId`) |
| `PUT` | `/api/notes/{id}` | Jegyzet frissítése (cím, tartalom, mappa) |
| `DELETE` | `/api/notes/{id}` | Jegyzet törlése |
| `GET` | `/api/attachments` | Az összes csatolmány |
| `POST` | `/api/attachments/{noteId}` | Csatolmány feltöltése (`multipart/form-data`, a fájl mezője: `file`) |