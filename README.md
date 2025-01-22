# TCP Tabanlı Güvenli Mesajlaşma Uygulaması

Proje, güvenli ve verimli bir mesajlaşma uygulaması geliştirmeyi amaçlayan bir TCP tabanlı gerçek zamanlı iletişim sistemidir. Uygulama, kullanıcıların birbirleriyle şifreli mesajlar aracılığıyla iletişim kurmasını sağlar. Mesajlar, uçtan uca şifreleme teknikleriyle korunur ve veritabanına kaydedilir. Ayrıca, çevrimdışı mesajlar kullanıcının çevrimiçi olduğu zaman gösterilebilir.

## Proje Özeti

Bu TCP tabanlı güvenli mesajlaşma uygulaması, aşağıdaki önemli özellikleri sunar:

- **Gerçek Zamanlı İletişim**: Kullanıcılar arasında anlık mesajlaşma.
- **Uçtan Uca Şifreleme**: Mesajlar asimetrik (RSA) ve simetrik (AES) şifreleme yöntemleriyle güvence altına alınır.
- **Veritabanı Yönetimi**: Mesajlar hash'leri alındıktan sonra veritabanına kaydedilir.
- **Çevrimdışı Mesajlar**: Çevrimdışı olan kullanıcılara gönderilen mesajlar, çevrimiçi olduklarında görüntülenebilir.
- **Grup Mesajlaşma**: Grup sohbetleri oluşturulabilir ve sadece belirli grup üyelerine mesaj iletilebilir.
- **Bireysel Mesajlaşma**: Kullanıcılar bireysel olarak güvenli mesajlaşabilirler.

## Kullanılan Teknolojiler

- **Programlama Dili**: C#
- **Protokol**: TCP (Transmission Control Protocol)
- **Veritabanı**: SQL Server
- **Geliştirme Ortamı**: Visual Studio
- **Şifreleme Yöntemleri**: RSA (asimetrik), AES (simetrik)
- **Soket Programlama**: TCP soketleri kullanılarak istemci ve sunucu arasında iletişim sağlanmıştır.

## Temel Özellikler

### 1. **Mesajlaşma ve Güvenlik**

- **Uçtan Uca Şifreleme**: Mesajlar RSA ile asimetrik olarak şifrelenir. Verimli iletişim için AES-256 algoritması ile simetrik şifreleme yapılır. Bu sayede yalnızca mesajı gönderen ve alan kullanıcılar mesajı çözebilir.
- **Mesaj Hash'leri**: Her gönderilen mesaj, SHA-256 algoritması ile hash'lenir ve veritabanına kaydedilir. Bu sayede mesajların orijinal hali güvenli bir şekilde saklanır.

### 2. **Mesajlaşma Türleri**

- **Grup Mesajlaşma**: Kullanıcılar grup sohbeti oluşturabilir, grup adı ve üye adlarını belirleyebilir. Mevcut gruplara katılım için yalnızca grup adı gereklidir.
- **Bireysel Mesajlaşma**: Kullanıcılar, belirli bir kişiyle bireysel olarak güvenli bir şekilde mesajlaşabilir. Mesajlar her zaman şifrelenmiş olarak iletilir.
- **Çevrimdışı Mesajlar**: Çevrimdışı olan kullanıcılara gönderilen mesajlar, kullanıcı çevrimiçi olduğunda otomatik olarak görüntülenir.

### 3. **Veritabanı Yönetimi**

- **Mesajların Kaydedilmesi**: Gönderilen her mesaj, hash'leri alınarak veritabanına kaydedilir. Bu özellik, mesajların güvenli bir şekilde saklanmasını ve bütünlüğünü sağlamak amacıyla tasarlanmıştır.
